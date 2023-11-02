using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

/// <summary>
/// An OpenAI Sentence Vectorizer.
/// </summary>
public class OpenAIVectorizerAttribute : VectorizerAttribute<string>
{
    private const string DefaultModel = "text-embedding-ada-002";

    /// <summary>
    /// The ModelId.
    /// </summary>
    public string ModelId { get; } = DefaultModel;

    /// <inheritdoc />
    public override VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public override int Dim => ModelId == DefaultModel ? 1536 : GetFloats("Probing model dimensions").Length;

    private IVectorizer<string>? _vectorizer;

    /// <inheritdoc />
    public override IVectorizer<string> Vectorizer
    {
        get
        {
            return _vectorizer ??= new OpenAIVectorizer(Configuration.Instance.OpenAiAuthorizationToken, ModelId, Dim);
        }
    }

    /// <inheritdoc />
    public override byte[] Vectorize(object obj)
    {
        var s = (string)obj;
        var floats = GetFloats(s);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }

    internal float[] GetFloats(string s)
    {
        return OpenAIVectorizer.GetFloats(s, ModelId, Configuration.Instance.OpenAiAuthorizationToken);
    }
}