using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.AllMiniLML6V2;

/// <summary>
/// 
/// </summary>
public class SentenceVectorizerAttribute : VectorizerAttribute<string>
{
    /// <inheritdoc />
    public override VectorType VectorType => Vectorizer.VectorType;

    /// <inheritdoc />
    public override int Dim => Vectorizer.Dim;

    /// <inheritdoc />
    public override byte[] Vectorize(object obj) => Vectorizer.Vectorize((string)obj);

    /// <inheritdoc />
    public override IVectorizer<string> Vectorizer => new SentenceVectorizer();
}