using System.Net.Http.Json;
using System.Text.Json;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

public class OpenAISentenceVectorizerAttribute : VectorizerAttribute
{
    private const string DefaultModel = "text-embedding-ada-002";
    public string ModelId { get; set; } = DefaultModel;
    public override VectorType VectorType => VectorType.FLOAT32; 

    public override int Dim => ModelId == DefaultModel ? 1536 : GetFloats("Probing model dimensions").Length;

    public override byte[] Vectorize(object obj)
    {
        var s = (string)obj;
        var floats = GetFloats(s);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }

    internal float[] GetFloats(string s)
    {
        return OpenAISentenceVectorizer.GetFloats(s, ModelId, Configuration.Instance.OpenAiAuthorizationToken);
    }
}