using System.Net.Http.Json;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.HuggingFace;

public class HuggingFaceApiSentenceVectorizer : VectorizerAttribute
{
    public string? ModelId { get; set; }
    public override VectorType VectorType => VectorType.FLOAT32;
    private int? _dim;

    public override int Dim
    {
        get
        {
            if (_dim is not null) return _dim.Value;
            const string testString = "This is a vector dimensionality probing query";
            var floats = GetFloats(testString);
            _dim = floats.Length;

            return _dim.Value;
        }
    }

    public override byte[] Vectorize(object obj)
    {
        var s = (string)obj;
        var floats = GetFloats(s);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }

    public float[] GetFloats(string s)
    {
        var client = Configuration.Instance.Client;
        var modelId = ModelId ?? Configuration.Instance["REDIS_OM_HF_MODEL_ID"];
        if (modelId is null) throw new InvalidOperationException("Model Id Required to use Hugging Face API.");

        var requestContent = JsonContent.Create(new
        {
            inputs = new string[] { s },
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
                { "Authorization", $"Bearer {Configuration.Instance.HuggingFaceAuthorizationToken}" }
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