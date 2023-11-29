using Redis.OM.Contracts;

namespace Redis.OM.Vectorizers;

/// <summary>
/// Static extensions for The RedisConnectionProvider.
/// </summary>
public static class RedisConnectionProviderExtensions
{
    
    /// <summary>
    /// Creates a Semantic Cache using the Hugging face model API 
    /// </summary>
    /// <param name="provider">The Connection Provider.</param>
    /// <param name="huggingFaceAuthToken">The API token for Hugging face.</param>
    /// <param name="threshold">The activation threshold.</param>
    /// <param name="modelId">The Model Id to use.</param>
    /// <param name="dim">The dimensionality of the tensors.</param>
    /// <param name="indexName">The Index name.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="ttl">The TTL</param>
    /// <returns></returns>
    public static ISemanticCache HuggingFaceSemanticCache(this IRedisConnectionProvider provider, string huggingFaceAuthToken, double threshold = .15, string modelId = "sentence-transformers/all-mpnet-base-v2", int dim = 768, string indexName = "HuggingFaceSemanticCache", string? prefix = null, long? ttl = null)
    {
        var vectorizer = new HuggingFaceVectorizer(huggingFaceAuthToken, modelId, dim);
        var connection = provider.Connection;
        var info = connection.GetIndexInfo(indexName);
        var cache = new SemanticCache(indexName, prefix ?? indexName, threshold, ttl, vectorizer, connection);
        if (info is null)
        {
            cache.CreateIndex();
        }

        return cache;
    }
    
    public static ISemanticCache OpenAISemanticCache(this IRedisConnectionProvider provider, string openAIAuthToken, double threshold = .15, string indexName = "OpenAISemanticCache", string? prefix = null, long? ttl = null)
    {
        var vectorizer = new OpenAIVectorizer(openAIAuthToken);
        var connection = provider.Connection;
        var info = connection.GetIndexInfo(indexName);
        var cache = new SemanticCache(indexName, prefix ?? indexName, threshold, ttl, vectorizer, connection);
        if (info is null)
        {
            cache.CreateIndex();
        }

        return cache;
    }

    public static ISemanticCache AzureOpenAISemanticCache(this IRedisConnectionProvider provider, string apiKey, string resourceName, string deploymentId, int dim, double threshold = .15, string indexName = "AzureOpenAISemanticCache", string? prefix = null, long? ttl = null)
    {
        var vectorizer = new AzureOpenAIVectorizer(apiKey, resourceName, deploymentId, dim);
        var connection = provider.Connection;
        var cache = new SemanticCache(indexName, prefix ?? indexName, threshold, ttl, vectorizer, connection);
        var info = connection.GetIndexInfo(indexName);
        if (info is null)
        {
            cache.CreateIndex();
        }

        return cache;
    }
}