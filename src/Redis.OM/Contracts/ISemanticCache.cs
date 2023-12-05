using System.Collections.Generic;
using System.Threading.Tasks;

namespace Redis.OM.Contracts
{
    /// <summary>
    /// An interface for interacting with a Semantic Cache.
    /// </summary>
    public interface ISemanticCache
    {
        /// <summary>
        /// Gets the index name of the cache.
        /// </summary>
        string IndexName { get; }

        /// <summary>
        /// Gets the prefix to be used for the keys.
        /// </summary>
        string Prefix { get; }

        /// <summary>
        /// Gets the threshold to be used for the keys.
        /// </summary>
        double Threshold { get; }

        /// <summary>
        /// Gets the Time to live for the keys added to the cache.
        /// </summary>
        long? Ttl { get; }

        /// <summary>
        /// Gets the vectorizer to use for the Semantic Cache.
        /// </summary>
        IVectorizer<string> Vectorizer { get; }

        /// <summary>
        /// Checks the cache to see if any close prompts have been added.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="maxNumResults">How many results to pull back at most (defaults to 10).</param>
        /// <returns>The responses.</returns>
        SemanticCacheResponse[] GetSimilar(string prompt, int maxNumResults = 10);

        /// <summary>
        /// Checks the cache to see if any close prompts have been added.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="maxNumResults">How many results to pull back at most (defaults to 10).</param>
        /// <returns>The responses.</returns>
        Task<SemanticCacheResponse[]> GetSimilarAsync(string prompt, int maxNumResults = 10);

        /// <summary>
        /// Stores the Prompt/response/metadata in Redis.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="response">The response.</param>
        /// <param name="metadata">The metadata.</param>
        void Store(string prompt, string response, object? metadata = null);

        /// <summary>
        /// Stores the Prompt/response/metadata in Redis.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="response">The response.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task StoreAsync(string prompt, string response, object? metadata = null);

        /// <summary>
        /// Deletes the cache from Redis.
        /// </summary>
        /// <param name="dropRecords">Whether or not to drop the records associated with the cache. Defaults to true.</param>
        void DeleteCache(bool dropRecords = true);

        /// <summary>
        /// Deletes the cache from Redis.
        /// </summary>
        /// <param name="dropRecords">Whether or not to drop the records associated with the cache. Defaults to true.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteCacheAsync(bool dropRecords = true);

        /// <summary>
        /// Creates the index for Semantic Cache.
        /// </summary>
        void CreateIndex();

        /// <summary>
        /// Creates the index for the Semantic Cache.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CreateIndexAsync();
    }
}