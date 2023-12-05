using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
[assembly: InternalsVisibleTo("Redis.OM.Vectorizers.Resnet18")]
namespace Redis.OM;

/// <summary>
/// Some Configuration Items.
/// </summary>
internal class Configuration
{
    /// <summary>
    /// Gets the configuration item at the given key.
    /// </summary>
    /// <param name="str"></param>
    public string? this[string str] => _settings[str];
    
    /// <summary>
    /// The bearer authorization token for Hugging Face's model API.
    /// </summary>
    public string HuggingFaceAuthorizationToken => _settings["REDIS_OM_HF_TOKEN"] ?? string.Empty;
    
    /// <summary>
    /// Bearer token for Open AI's API.
    /// </summary>
    public string OpenAiAuthorizationToken => _settings["REDIS_OM_OAI_TOKEN"] ?? string.Empty;
   
    /// <summary>
    /// Azure OpenAI Api Key.
    /// </summary>
    public string AzureOpenAIApiKey => _settings["REDIS_OM_AZURE_OAI_TOKEN"] ?? string.Empty;
    
    /// <summary>
    /// Hugging Face Model Id 
    /// </summary>
    public string ModelId => _settings["REDIS_OM_HF_MODEL_ID"] ?? string.Empty;
    
    /// <summary>
    /// Base Address for Hugging Face Feature Extraction API
    /// </summary>
    public string HuggingFaceBaseAddress => _settings["REDIS_OM_HF_FEATURE_EXTRACTION_URL"] ?? string.Empty; 
    
    private const string DefaultHuggingFaceApiUrl = "https://api-inference.huggingface.co";

    private const string DefaultOpenAiApiUrl = "https://api.openai.com";
    
    /// <summary>
    /// URL for OpenAI API.
    /// </summary>
    public string OpenAiApiUrl => _settings["REDIS_OM_OAI_API_URL"] ?? String.Empty;
    
    private readonly IConfiguration _settings;

    private static readonly object LockObject = new ();
    private static Configuration? _instance;

    /// <summary>
    /// Common HTTP Client.
    /// </summary>
    public readonly HttpClient Client;

    /// <summary>
    /// Singleton Instance.
    /// </summary>
    public static Configuration Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            lock (LockObject)
            {
                _instance ??= new Configuration();
            }

            return _instance;
        }
    }

    internal Configuration()
    {
        var builder = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"REDIS_OM_HF_FEATURE_EXTRACTION_URL", DefaultHuggingFaceApiUrl},
                {"REDIS_OM_OAI_API_URL", DefaultOpenAiApiUrl},
                {"REDIS_OM_HF_TOKEN", Environment.GetEnvironmentVariable("REDIS_OM_HF_TOKEN")},
                {"REDIS_OM_OAI_TOKEN", Environment.GetEnvironmentVariable("REDIS_OM_OAI_TOKEN")},
                {"REDIS_OM_AZURE_OAI_TOKEN", Environment.GetEnvironmentVariable("REDIS_OM_AZURE_OAI_TOKEN")}
            })
        .AddJsonFile("settings.json", true, true)
        .AddJsonFile("appsettings.json", true, true);
        _settings = builder.Build();
        Client = new HttpClient();
    }
}