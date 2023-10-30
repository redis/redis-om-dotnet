using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace Redis.OM;

public class Configuration
{
    public string? this[string str] => _settings[str];
    public string HuggingFaceAuthorizationToken => _settings["REDIS_OM_HF_TOKEN"] ?? string.Empty;
    public string OpenAiAuthorizationToken => _settings["REDIS_OM_OAI_TOKEN"] ?? string.Empty;
    public string AzureOpenAIApiKey => _settings["REDIS_OM_AZURE_OAI_TOKEN"] ?? string.Empty;
    public string ModelId => _settings["REDIS_OM_HF_MODEL_ID"] ?? string.Empty;
    public string HuggingFaceBaseAddress => _settings["REDIS_OM_HF_FEATURE_EXTRACTION_URL"] ?? string.Empty; 
    
    private const string DefaultHuggingFaceApiUrl = "https://api-inference.huggingface.co";

    private const string DefaultOpenAiApiUrl = "https://api.openai.com";
    public string OpenAiApiUrl => _settings["REDIS_OM_OAI_API_URL"] ?? String.Empty;
    
    private readonly IConfiguration _settings;

    private static readonly object LockObject = new ();
    private static Configuration? _instance;

    public readonly HttpClient Client;

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