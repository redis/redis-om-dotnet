using System.Net.Http.Json;
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
        return GetFloats(str).SelectMany(BitConverter.GetBytes).ToArray();
    }
    
    public float[] GetFloats(string s)
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
                new Uri($"{Configuration.Instance.HuggingFaceBaseAddress}/pipeline/feature-extraction/{ModelId}"),
            Headers =
            {
                { "Authorization", $"Bearer {_huggingFaceAuthToken}" }
            }
        };
        
        var res = client.SendAsync(request).Result;
        var floats = res.Content.ReadFromJsonAsync<float[][]>().Result;
        if (floats is null)
        {
            throw new Exception("Did not receive a response back from HuggingFace");
        }

        return floats.First();
    }
}