using System.Net.Http.Json;
using System.Text.Json;
using Redis.OM.Modeling;
using Redis.OM.Vectorizers.HuggingFace;

namespace Redis.OM.OpenAI;

public class OpenAISentenceVectorizerAttribute : VectorizerAttribute
{
    private const string DefaultModel = "text-embedding-ada-002";
    public string ModelId { get; set; } = DefaultModel;
    public override VectorType VectorType { get; }

    public override int Dim
    {
        get
        {
            if (ModelId == DefaultModel)
            {
                return 1536;
            }

            return GetFloats("this is a test string").Length;
        }
    }

    public override byte[] Vectorize(object obj)
    {
        var s = (string)obj;
        var floats = GetFloats(s);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }

    internal float[] GetFloats(string s)
    {
        var client = Configuration.Instance.Client;
        var requestContent = JsonContent.Create(
            new
            {
                input = s,
                model = ModelId
            });

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Configuration.Instance.OpenAiApiUrl}/v1/embeddings"),
            Content = requestContent,
            Headers = { { "Authorization", $"Bearer {Configuration.Instance.OpenAiAuthorizationToken}" } }
        };

        var res = client.SendAsync(request).Result;
        if (!res.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Open AI did not respond with a positive error code: {res.StatusCode}, {res.ReasonPhrase}");
        }
        var jsonObj = res.Content.ReadFromJsonAsync<JsonElement>().Result;

        
        if (!jsonObj.TryGetProperty("data", out var data))
        {
            throw new Exception("Malformed Response");
        }

        if (data.GetArrayLength() < 1 ||  !data[0].TryGetProperty("embedding", out var embedding))
        {
            throw new Exception("Malformed Response");
        }

        return embedding.Deserialize<float[]>()!;
    }
}