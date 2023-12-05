
using System.Net.Http.Json;
using System.Text.Json;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

/// <summary>
/// Vectorizer for Azure's OpenAI REST API
/// </summary>
public class AzureOpenAIVectorizer : IVectorizer<string>
{
    private readonly string _apiKey;
    private readonly string _resourceName;
    private readonly string _deploymentName;
    
    /// <summary>
    /// Initializes vectorizer
    /// </summary>
    /// <param name="apiKey">The Vectorizers API Key</param>
    /// <param name="resourceName">The Azure Resource Name.</param>
    /// <param name="deploymentName">The Azure Deployment Name.</param>
    /// <param name="dim">The dimensions of the model addressed by this resource/deployment.</param>
    public AzureOpenAIVectorizer(string apiKey, string resourceName, string deploymentName, int dim)
    {
        _apiKey = apiKey;
        _resourceName = resourceName;
        _deploymentName = deploymentName;
        Dim = dim;
    }

    /// <inheritdoc />
    public VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public int Dim { get; }

    /// <inheritdoc />
    public byte[] Vectorize(string str) => GetFloats(str, _resourceName, _deploymentName, _apiKey).SelectMany(BitConverter.GetBytes).ToArray();
    
    internal static float[] GetFloats(string s, string resourceName, string deploymentName, string apiKey)
    {
        var client = Configuration.Instance.Client;
        var requestContent = JsonContent.Create(new { input = s });

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(
                $"https://{resourceName}.openai.azure.com/openai/deployments/{deploymentName}/embeddings?api-version=2023-05-15"),
            Content = requestContent,
            Headers = { { "api-key", apiKey } }
        };

        var res = client.Send(request);
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