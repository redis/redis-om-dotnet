using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching.Query;

namespace Redis.OM
{
    /// <summary>
    /// A semantic cache for Large Language Models.
    /// </summary>
    public class SemanticCache : ISemanticCache
    {
        private readonly IRedisConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticCache"/> class.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <param name="prefix">The prefix for indexed items.</param>
        /// <param name="threshold">The threshold to check against..</param>
        /// <param name="ttl">The Time To Live for a record inserted.</param>
        /// <param name="vectorizer">The vectorizer to use.</param>
        /// <param name="connection">The connection to redis.</param>
        public SemanticCache(string indexName, string prefix, double threshold, long? ttl, IVectorizer<string> vectorizer, IRedisConnection connection)
        {
            IndexName = indexName;
            Prefix = prefix;
            Threshold = threshold;
            Ttl = ttl;
            Vectorizer = vectorizer;
            _connection = connection;
        }

        /// <inheritdoc/>
        public string IndexName { get; }

        /// <inheritdoc/>
        public string Prefix { get; }

        /// <inheritdoc/>
        public double Threshold { get; }

        /// <inheritdoc/>
        public long? Ttl { get; }

        /// <inheritdoc/>
        public IVectorizer<string> Vectorizer { get; }

        /// <inheritdoc/>
        public SemanticCacheResponse[] GetSimilar(string prompt, int maxNumResults = 10)
        {
            var query = BuildCheckQuery(prompt, maxNumResults);
            var res = (RedisReply[])_connection.Execute("FT.SEARCH", query.SerializeQuery());
            return BuildResponse(res);
        }

        /// <inheritdoc/>
        public async Task<SemanticCacheResponse[]> GetSimilarAsync(string prompt, int maxNumResults = 10)
        {
            var query = BuildCheckQuery(prompt, maxNumResults);
            var res = (RedisReply[])await _connection.ExecuteAsync("FT.SEARCH", query.SerializeQuery()).ConfigureAwait(false);
            return BuildResponse(res);
        }

        /// <inheritdoc/>
        public void Store(string prompt, string response, object? metadata = null)
        {
            var key = $"{Prefix}:{Sha256Hash(prompt)}";
            var hash = BuildDocumentHash(prompt, response, metadata);
            if (Ttl is not null)
            {
                _connection.HSet(key, TimeSpan.FromMilliseconds((double)Ttl), hash.ToArray());
            }
            else
            {
                _connection.HSet(key, hash.ToArray());
            }
        }

        /// <inheritdoc/>
        public Task StoreAsync(string prompt, string response, object? metadata = null)
        {
            var key = $"{Prefix}:{Sha256Hash(prompt)}";
            var hash = BuildDocumentHash(prompt, response, metadata);
            return Ttl is not null ? _connection.HSetAsync(key, TimeSpan.FromMilliseconds((double)Ttl), hash.ToArray()) : _connection.HSetAsync(key, hash.ToArray());
        }

        /// <inheritdoc />
        public void DeleteCache(bool dropRecords = true)
        {
            try
            {
                if (dropRecords)
                {
                    _connection.Execute("FT.DROPINDEX", IndexName, "DD");
                }
                else
                {
                    _connection.Execute("FT.DROPINDEX", IndexName);
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Unknown Index name"))
                {
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public Task DeleteCacheAsync(bool dropRecords = true)
        {
            try
            {
                return dropRecords ? _connection.ExecuteAsync("FT.DROPINDEX", IndexName, "DD") : _connection.ExecuteAsync("FT.DROPINDEX", IndexName);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Unknown Index name"))
                {
                    throw;
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void CreateIndex()
        {
            try
            {
                var serializedParams = SerializedIndexArgs();
                _connection.Execute("FT.CREATE", serializedParams);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index already exists"))
                {
                    return;
                }

                throw;
            }
        }

        /// <inheritdoc />
        public Task CreateIndexAsync()
        {
            try
            {
                var serializedParams = SerializedIndexArgs();
                return _connection.ExecuteAsync("FT.CREATE", serializedParams);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index already exists"))
                {
                    return Task.CompletedTask;
                }

                throw;
            }
        }

        private static string Sha256Hash(string value)
        {
            StringBuilder sb = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (byte b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }

            return sb.ToString();
        }

        private RedisQuery BuildCheckQuery(string prompt, int maxNumResults)
        {
            var query = new RedisQuery(IndexName);
            query.QueryText = "@embedding:[VECTOR_RANGE $0 $1]=>{$YIELD_DISTANCE_AS: semantic_score}";
            query.Parameters.Add(Threshold);
            query.Parameters.Add(Vectorizer.Vectorize(prompt));
            if (maxNumResults != 10)
            {
                query.Limit = new SearchLimit { Number = maxNumResults, Offset = 0 };
            }

            return query;
        }

        private SemanticCacheResponse[] BuildResponse(RedisReply[] res)
        {
            List<SemanticCacheResponse> results = new List<SemanticCacheResponse>();
            for (int i = 1; i < res.Length; i += 2)
            {
                var key = (string)res[i];
                var hashArr = (RedisReply[])res[i + 1];
                Dictionary<string, string> hash = new Dictionary<string, string>();
                for (var j = 0; j < hashArr.Length; j += 2)
                {
                    hash.Add(hashArr[j], hashArr[j + 1]);
                }

                var score = double.Parse(hash["semantic_score"], CultureInfo.InvariantCulture);
                var response = hash["response"];
                hash.TryGetValue("metadata", out var metadata);
                results.Add(new SemanticCacheResponse(key, response, score, metadata));
            }

            return results.ToArray();
        }

        private Dictionary<string, object> BuildDocumentHash(string prompt, string response, object? metadata)
        {
            var bytes = Vectorizer.Vectorize(prompt);
            Dictionary<string, object> hash = new Dictionary<string, object>();
            hash.Add("embedding", bytes);
            hash.Add("response", response);
            hash.Add("prompt", prompt);
            if (metadata is not null)
            {
                hash.Add("metadata", metadata);
            }

            return hash;
        }

        private object[] SerializedIndexArgs()
        {
            return new object[] { IndexName, nameof(Prefix), 1, Prefix, "SCHEMA", "embedding", "VECTOR", "FLAT", 6, "DIM", Vectorizer.Dim, "TYPE", Vectorizer.VectorType.AsRedisString(), "DISTANCE_METRIC", "COSINE", };
        }
    }
}