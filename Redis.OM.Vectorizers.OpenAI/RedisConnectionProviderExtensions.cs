using Redis.OM.Contracts;

namespace Redis.OM;

public static class RedisConnectionProviderExtensions
{
    public static ISemanticCache OpenAISemanticCache(this IRedisConnectionProvider provider, string openAIAuthToken, double threshold = .15, string indexName = "OpenAISemanticCache", string? prefix = null, long? ttl = null)
    {
        var vectorizer = new OpenAISentenceVectorizer(openAIAuthToken);
        var connection = provider.Connection;
        var info = connection.GetIndexInfo(indexName);
        var cache = new SemanticCache(indexName, prefix ?? indexName, threshold, ttl, vectorizer, connection);
        if (info is null)
        {
            cache.CreateIndex();
        }

        return cache;
    }
}