using System.Reflection;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Vectorizers.AllMiniLML6V2.Tokenizers;

namespace Redis.OM.Vectorizers.AllMiniLML6V2;

/// <summary>
/// A vectorizer to Vectorize sentences using ALl Mini LM L6 V2 Model.
/// </summary>
public class SentenceVectorizer : IVectorizer<string>
{
    /// <inheritdoc />
    public VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public int Dim => 384;
    private static Lazy<TokenizerBase> Tokenizer => new Lazy<TokenizerBase>(AllMiniLML6V2Tokenizer.Create);
    private static Lazy<InferenceSession> InferenceSession => new Lazy<InferenceSession>(LoadInferenceSession);

    private static InferenceSession LoadInferenceSession()
    {
        var file = "Redis.OM.Vectorizers.AllMiniLML6V2.Resources.model.onnx";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file);
        if (stream is null)
        {
            throw new InvalidOperationException("Could not find Model resource");
        }

        var resourceBytes = new byte[stream.Length];
        _ = stream.Read(resourceBytes, 0, resourceBytes.Length);
        return new InferenceSession(resourceBytes);
    }

    /// <inheritdoc />
    public byte[] Vectorize(string obj)
    {
        return Vectorize(new[] { obj })[0].SelectMany(BitConverter.GetBytes).ToArray();
    }

     private static Lazy<string[]> OutputNames => new (() => InferenceSession.Value.OutputMetadata.Keys.ToArray());

    /// <summary>
    /// Vectorizers an array of sentences (which are vectorized individually).
    /// </summary>
    /// <param name="sentences">The Sentences</param>
    /// <returns></returns>
    public static float[][] Vectorize(string[] sentences)
    {
        const int MaxTokens = 512;
        var numSentences = sentences.Length;

        var tokenized = sentences.Select(x=>Tokenizer.Value.Tokenize(x)).ToArray();

        var seqLen = tokenized.Max(t => Math.Min(MaxTokens, t.Count));

        List<(long[] InputIds, long[] TokenTypeIds, long[] AttentionMask)> encoded = tokenized.Select(tokens =>
        {
            var padding = Enumerable.Repeat(0L, seqLen - Math.Min(MaxTokens, tokens.Count)).ToList();

            var tokenIndexes = tokens.Take(MaxTokens).Select(token => (long)token.VocabularyIndex).Concat(padding).ToArray();
            var segmentIndexes = tokens.Take(MaxTokens).Select(token => token.SegmentIndex).Concat(padding).ToArray();
            var inputMask = tokens.Take(MaxTokens).Select(_ => 1L).Concat(padding).ToArray();
            return (tokenIndexes, TokenTypeIds: segmentIndexes, inputMask);
        }).ToList();
        var tokenCount = encoded.First().InputIds.Length;

        long[] flattenIDs           = new long[encoded.Sum(s => s.InputIds.Length)];
        long[] flattenAttentionMask = new long[encoded.Sum(s => s.AttentionMask.Length)];
        long[] flattenTokenTypeIds  = new long[encoded.Sum(s => s.TokenTypeIds.Length)];
        
        var flattenIDsSpan           = flattenIDs.AsSpan();
        var flattenAttentionMaskSpan = flattenAttentionMask.AsSpan();
        var flattenTokenTypeIdsSpan  = flattenTokenTypeIds.AsSpan();

        foreach (var (InputIds, TokenTypeIds, AttentionMask) in encoded)
        {
            InputIds.AsSpan().CopyTo(flattenIDsSpan);
            flattenIDsSpan = flattenIDsSpan.Slice(InputIds.Length);
            
            AttentionMask.AsSpan().CopyTo(flattenAttentionMaskSpan);
            flattenAttentionMaskSpan = flattenAttentionMaskSpan.Slice(AttentionMask.Length);
            
            TokenTypeIds.AsSpan().CopyTo(flattenTokenTypeIdsSpan);
            flattenTokenTypeIdsSpan = flattenTokenTypeIdsSpan.Slice(TokenTypeIds.Length);
        }

        var dimensions = new[] { numSentences, tokenCount };

        var input = new []
        {
            NamedOnnxValue.CreateFromTensor("input_ids",      new DenseTensor<long>(flattenIDs,          dimensions)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(flattenAttentionMask,dimensions)),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(flattenTokenTypeIds, dimensions))
        };

        using var runOptions = new RunOptions();

        using var output = InferenceSession.Value.Run(input, OutputNames.Value, runOptions);

        var output_pooled = MeanPooling((DenseTensor<float>)output.First().Value, encoded);
        var output_pooled_normalized = Normalize(output_pooled);
        
        const int embDim = 384;

        var outputFlatten = new float[sentences.Length][];

        for(int s = 0; s < sentences.Length; s++)
        {
            var emb = new float[embDim];
            outputFlatten[s] = emb;

            for (int i = 0; i < embDim; i++)
            {
                emb[i] = output_pooled_normalized[s, i];
            }
        }

        return outputFlatten;
    }
     
    internal static DenseTensor<float> Normalize(DenseTensor<float> input_dense, float eps = 1e-12f)
    {
        var sentencesCount = input_dense.Dimensions[0];
        var hiddenStates   = input_dense.Dimensions[1];

        var denom_dense = new float [sentencesCount];

        for (int s = 0; s < sentencesCount; s++)
        {
            for (int i = 0; i < hiddenStates; i++)
            {
                denom_dense[s] += input_dense[s, i] * input_dense[s, i];
            }

            denom_dense[s] = MathF.Max(MathF.Sqrt(denom_dense[s]), eps);
        }

        for (int s = 0; s < sentencesCount; s++)
        {
            var invNorm = 1 / denom_dense[s];

            for (int i = 0; i < hiddenStates; i++)
            {
                input_dense[s, i] *= invNorm;
            }
        }

        return input_dense;
    }


    internal static DenseTensor<float> MeanPooling(DenseTensor<float> token_embeddings_dense, List<(long[] InputIds, long[] TokenTypeIds, long[] AttentionMask)> encodedSentences, float eps = 1e-9f)
    {
        var sentencesCount = token_embeddings_dense.Dimensions[0];
        var sentenceLength = token_embeddings_dense.Dimensions[1];
        var hiddenStates = token_embeddings_dense.Dimensions[2];

        var result = new DenseTensor<float>(new[] { sentencesCount, hiddenStates });

        for (int s = 0; s < sentencesCount; s++)
        {
            var maskSum = 0f;

            var attentionMask = encodedSentences[s].AttentionMask;

            for (int t = 0; t < sentenceLength; t++)
            {
                maskSum += attentionMask[t];

                for (int i = 0; i < hiddenStates; i++)
                {
                    result[s, i] += token_embeddings_dense[s, t, i] * attentionMask[t];
                }
            }

            var invSum = 1f / MathF.Max(maskSum, eps);
            for (int i = 0; i < hiddenStates; i++)
            {
                result[s, i] *= invSum;
            }
        }

        return result;
    }
}