using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

/// <summary>
/// An attribute that provides a Hugging Face API Sentence Vectorizer.
/// </summary>
public class HuggingFaceVectorizerAttribute : VectorizerAttribute<string>
{
    /// <summary>
    /// The Model Id.
    /// </summary>
    public string? ModelId { get; set; }

    private IVectorizer<string>? _vectorizer;

    /// <inheritdoc />
    public override IVectorizer<string> Vectorizer
    {
        get
        {
            if (_vectorizer is null)
            {
                var modelId = ModelId ?? Configuration.Instance["REDIS_OM_HF_MODEL_ID"];
                if (modelId is null)
                {
                    throw new InvalidOperationException("Need a Model ID in order to process vector");
                }
            
                _vectorizer = new HuggingFaceVectorizer(Configuration.Instance.HuggingFaceAuthorizationToken, modelId, Dim);
            }

            return _vectorizer;
        }
    }


    /// <inheritdoc />
    public override VectorType VectorType => VectorType.FLOAT32;
    private int? _dim;

    /// <inheritdoc />
    public override int Dim
    {
        get
        {
            if (_dim is not null) return _dim.Value;
            const string testString = "This is a vector dimensionality probing query";
            var floats = GetFloats(testString);
            _dim = floats.Length;

            return _dim.Value;
        }
    }

    /// <inheritdoc />
    public override byte[] Vectorize(object obj)
    {
        if (obj is not string s)
        {
            throw new ArgumentException("Object must be a string", nameof(obj));
        }
        
        var floats = GetFloats(s);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }

    /// <summary>
    /// Gets the embedded floats of the string from the HuggingFace API.
    /// </summary>
    /// <param name="s">the string.</param>
    /// <returns>the embedding's floats.</returns>
    /// <exception cref="InvalidOperationException">thrown if model id is not populated.</exception>
    public float[] GetFloats(string s)
    {
        var modelId = ModelId ?? Configuration.Instance["REDIS_OM_HF_MODEL_ID"];
        if (modelId is null) throw new InvalidOperationException("Model Id Required to use Hugging Face API.");
        return HuggingFaceVectorizer.GetFloats(s, modelId, Configuration.Instance.HuggingFaceAuthorizationToken);
    }
}