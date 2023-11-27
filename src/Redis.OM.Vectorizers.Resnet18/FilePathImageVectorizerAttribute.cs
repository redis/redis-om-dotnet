using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.Resnet18;

public class FilePathImageVectorizerAttribute : VectorizerAttribute<string>
{
    public override byte[] Vectorize(object obj) => Vectorizer.Vectorize((string)obj);
    public override VectorType VectorType => Vectorizer.VectorType;
    public override int Dim => Vectorizer.Dim;
    public override IVectorizer<string> Vectorizer => new FilePathImageVectorizer();   
}