using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.AllMiniLML6V2;

public class SentenceVectorizerAttribute : VectorizerAttribute<string>
{
    public override VectorType VectorType => Vectorizer.VectorType;
    public override int Dim => Vectorizer.Dim;
    public override byte[] Vectorize(object obj) => Vectorizer.Vectorize((string)obj);

    public override IVectorizer<string> Vectorizer => new SentenceVectorizer();
}