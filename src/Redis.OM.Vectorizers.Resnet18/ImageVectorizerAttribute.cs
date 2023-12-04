using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.Resnet18;

public class ImageVectorizerAttribute : VectorizerAttribute<string>
{
    public override VectorType VectorType => Vectorizer.VectorType;
    public override int Dim => Vectorizer.Dim;
    public override byte[] Vectorize(object obj) => Vectorizer.Vectorize((string)obj);
    public override IVectorizer<string> Vectorizer { get; } = new ImageVectorizer();
}