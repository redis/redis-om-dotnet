using System.Net.Http.Json;
using System.Text.Json;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

/// <summary>
/// Vectorizer for HuggingFace API.
/// </summary>
public class HuggingFaceVectorizer : IVectorizer<string>
{
    /// <summary>
    /// Initializes the Vectorizer.
    /// </summary>
    /// <param name="authToken">Auth token.</param>
    /// <param name="modelId">Model Id.</param>
    /// <param name="dim">Dimensions for the output tensors of the model.</param>
    public HuggingFaceVectorizer(string authToken, string modelId, int dim)
    {
        _huggingFaceAuthToken = authToken;
        ModelId = modelId;
        Dim = dim;
    }
    
    private readonly string _huggingFaceAuthToken;

    /// <summary>
    /// The Model Id.
    /// </summary>
    public string ModelId { get; }

    /// <inheritdoc />
    public VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public int Dim { get; }

    /// <inheritdoc />
    public byte[] Vectorize(string str)
    {
        return GetFloats(str, ModelId, _huggingFaceAuthToken).SelectMany(BitConverter.GetBytes).ToArray();
    }
    
    /// <summary>
    /// Gets the floats for the sentence.
    /// </summary>
    /// <param name="s">the string.</param>
    /// <param name="modelId">The Model Id.</param>
    /// <param name="huggingFaceAuthToken">The HF token.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static float[] GetFloats(string s, string modelId, string huggingFaceAuthToken)
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