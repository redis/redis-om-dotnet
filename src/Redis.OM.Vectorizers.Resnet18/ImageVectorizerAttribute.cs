using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.Resnet18;

/// <summary>
/// A Vectorizer Attribute for encoding images
/// </summary>
public class ImageVectorizerAttribute : VectorizerAttribute<string>
{
    /// <inheritdoc />
    public override VectorType VectorType => Vectorizer.VectorType;

    /// <inheritdoc />
    public override int Dim => Vectorizer.Dim;

    /// <inheritdoc />
    public override byte[] Vectorize(object obj) => Vectorizer.Vectorize((string)obj);

    /// <inheritdoc />
    public override IVectorizer<string> Vectorizer { get; } = new ImageVectorizer();
}