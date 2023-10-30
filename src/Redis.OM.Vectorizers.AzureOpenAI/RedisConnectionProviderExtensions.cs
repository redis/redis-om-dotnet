using Redis.OM.Contracts;

namespace Redis.OM.Vectorizers.AzureOpenAI;

public static class RedisConnectionProviderExtensions
{
    public static ISemanticCache AzureOpenAISemanticCache(this IRedisConnectionProvider provider, string apiKey, string resourceName, string deploymentId, int dim, double threshold = .15, string indexName = "AzureOpenAISemanticCache", string? prefix = null, long? ttl = null)
    {
        var vectorizer = new AzureOpenAISentenceVectorizer(apiKey, resourceName, deploymentId, dim);
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