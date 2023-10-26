namespace Redis.OM
{
    /// <summary>
    /// A response to a Semantic Cache Query.
    /// </summary>
    public class SemanticCacheResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticCacheResponse"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="response">The string response.</param>
        /// <param name="score">The Score.</param>
        /// <param name="metaData">The metadata.</param>
        internal SemanticCacheResponse(string key, string response, double score, object? metaData)
        {
            Key = key;
            Response = response;
            Score = score;
            MetaData = metaData;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public string Response { get; }

        /// <summary>
        /// Gets the score.
        /// </summary>
        public double Score { get;  }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public object? MetaData { get; }

        /// <summary>
        /// Converts response to string implicitly.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>The response string.</returns>
        public static implicit operator string(SemanticCacheResponse response) => response.Response;
    }
}