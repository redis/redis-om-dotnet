using Redis.OM.Contracts;

namespace Redis.OM.Vectorizers.AllMiniLML6V2;

/// <summary>
/// Static extensions for The RedisConnectionProvider.
/// </summary>
public static class RedisConnectionProviderExtensions
{
    /// <summary>
    /// Creates a Semantic Cache using the All-MiniLM-L6-v2 Vectorizer
    /// </summary>
    /// <param name="provider">The connection provider.</param>
    /// <param name="indexName">The Index that the cache will be stored in.</param>
    /// <param name="threshold">The threshold that will be considered a match</param>
    /// <param name="prefix">The Prefix.</param>
    /// <param name="ttl">The Time to Live for a record stored in Redis.</param>
    /// <returns></returns>
    public static ISemanticCache AllMiniLML6V2SemanticCache(this IRedisConnectionProvider provider, string indexName="AllMiniLML6V2SemanticCache", double threshold = .15, string? prefix = null, long? ttl = null)
    {
        var vectorizer = new SentenceVectorizer();
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