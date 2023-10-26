using Redis.OM.Contracts;
using Redis.OM.Vectorizers.HuggingFace;

namespace Redis.OM;

public static class RedisConnectionProviderExtensions
{
    public static ISemanticCache HuggingFaceSemanticCache(this IRedisConnectionProvider provider, string huggingFaceAuthToken, double threshold = .15, string modelId = "sentence-transformers/all-mpnet-base-v2", int dim = 768, string indexName = "HuggingFaceSemanticCache", string? prefix = null, long? ttl = null)
    {
        var vectorizer = new HuggingFaceApiSentenceVectorizer(huggingFaceAuthToken, modelId, dim);
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