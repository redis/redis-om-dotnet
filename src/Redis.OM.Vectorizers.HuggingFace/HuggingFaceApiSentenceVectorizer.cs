using System.Net.Http.Json;
using System.Text.Json;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.HuggingFace;

public class HuggingFaceApiSentenceVectorizer : IVectorizer<string>
{
    public HuggingFaceApiSentenceVectorizer(string authToken, string modelId, int dim)
    {
        _huggingFaceAuthToken = authToken;
        ModelId = modelId;
        Dim = dim;
    }
    
    private readonly string _huggingFaceAuthToken;
    public string ModelId { get; }
    public VectorType VectorType => VectorType.FLOAT32;
    
    public int Dim { get; }
    public byte[] Vectorize(string str)
    {
        return GetFloats(str, ModelId, _huggingFaceAuthToken).SelectMany(BitConverter.GetBytes).ToArray();
    }
    
    public static float[] GetFloats(string s, string modelId, string huggingFaceAuthToken)
    {
        var client = Configuration.Instance.Client;
        var requestContent = JsonContent.Create(new
        {
            inputs = new [] { s },
            options = new { wait_for_model = true }
        });

        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            Content = requestContent,
            RequestUri =
                new Uri($"{Configuration.Instance.HuggingFaceBaseAddress}/pipeline/feature-extraction/{modelId}"),
            Headers =
            {
                { "Authorization", $"Bearer {huggingFaceAuthToken}" }
            }
        };
        
        var res = client.Send(request);
        var floats = JsonSerializer.Deserialize<float[][]>(RedisOMHttpUtil.ReadJsonSync(res));
        if (floats is null)
        {
            throw new Exception("Did not receive a response back from HuggingFace");
        }

        return floats.First();
    }
}