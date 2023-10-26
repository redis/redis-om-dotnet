using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.OpenAI;

public class OpenAISentenceVectorizer : IVectorizer<string>
{
    private readonly string _openAIAuthToken;
    private readonly string _model;

    public OpenAISentenceVectorizer(string openAIAuthToken, string model = "text-embedding-ada-002", int dim = 1536)
    {
        _openAIAuthToken = openAIAuthToken;
        _model = model;
        Dim = dim;
    }

    public VectorType VectorType => VectorType.FLOAT32;
    public int Dim { get; }
    public byte[] Vectorize(string obj)
    {
        throw new NotImplementedException();
    }
}