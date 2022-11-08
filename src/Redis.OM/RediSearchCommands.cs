using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;
using StackExchange.Redis;

namespace Redis.OM
{
    /// <summary>
    /// extension methods for redisearch.
    /// </summary>
    public static class RediSearchCommands
    {
        /// <summary>
        /// Search redis with the given query.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="query">the query to use in the search.</param>
        /// <typeparam name="T">the type.</typeparam>
        /// <returns>A typed search response.</returns>
        public static SearchResponse<T> Search<T>(this IRedisConnection connection, RedisQuery query)
            where T : notnull
        {
            var res = connection.Execute("FT.SEARCH", query.SerializeQuery());
            return new SearchResponse<T>(res);
        }

        /// <summary>
        /// Search redis with the given query.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="query">the query to use in the search.</param>
        /// <typeparam name="T">the type.</typeparam>
        /// <returns>A typed search response.</returns>
        public static async Task<SearchResponse<T>> SearchAsync<T>(this IRedisConnection connection, RedisQuery query)
            where T : notnull
        {
            var res = await connection.ExecuteAsync("FT.SEARCH", query.SerializeQuery());
            return new SearchResponse<T>(res);
        }

        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to use for creating the index.</param>
        /// <returns>whether the index was created or not.</returns>
        public static bool CreateIndex(this IRedisConnection connection, Type type)
        {
            try
            {
                var serializedParams = type.SerializeIndex();
                connection.Execute("FT.CREATE", serializedParams);
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index already exists"))
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to use for creating the index.</param>
        /// <returns>whether the index was created or not.</returns>
        public static async Task<bool> CreateIndexAsync(this IRedisConnection connection, Type type)
        {
            try
            {
                var serializedParams = type.SerializeIndex();
                await connection.ExecuteAsync("FT.CREATE", serializedParams);
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index already exists"))
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Get index information.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type that maps to the index.</param>
        /// <returns>Strong-typed result of FT.INFO idx.</returns>
        public static RedisIndexInfo? GetIndexInfo(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().First();
                var redisReply = connection.Execute("FT.INFO", indexName);
                var redisIndexInfo = new RedisIndexInfo(redisReply);
                return redisIndexInfo;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                {
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Get index information.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type that maps to the index.</param>
        /// <returns>Strong-typed result of FT.INFO idx.</returns>
        public static async Task<RedisIndexInfo?> GetIndexInfoAsync(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().First();
                var redisReply = await connection.ExecuteAsync("FT.INFO", indexName);
                var redisIndexInfo = new RedisIndexInfo(redisReply);
                return redisIndexInfo;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                {
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Deletes an index.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to drop the index for.</param>
        /// <returns>whether the index was dropped or not.</returns>
        public static async Task<bool> DropIndexAsync(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().First();
                await connection.ExecuteAsync("FT.DROPINDEX", indexName);
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Deletes an index.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to drop the index for.</param>
        /// <returns>whether the index was dropped or not.</returns>
        public static bool DropIndex(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().First();
                connection.Execute("FT.DROPINDEX", indexName);
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Deletes an index. And drops associated records.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to drop the index for.</param>
        /// <returns>whether the index was dropped or not.</returns>
        public static bool DropIndexAndAssociatedRecords(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().First();
                connection.Execute("FT.DROPINDEX", indexName, "DD");
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Get completion suggestions for a prefix.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="item">is suggestion dictionary key.</param>
        /// <param name="prefix">prefix to complete on.</param>
        /// <param name="fuzzy">Optional type performs a fuzzy prefix search.</param>
        /// <param name="max">Optional type limits the results to a maximum of num (default: 5).</param>
        /// <param name="withscores">Optional type also returns the score of each suggestion.</param>
        /// <param name="withpayloads">Optional type returns optional payloads saved along with the suggestions.</param>
        /// <returns>List of string suggestions for prefix.</returns>
        public static List<string> SuggestionGet(this IRedisConnection connection, object item, string prefix, bool? fuzzy = false, int? max = 0, bool? withscores = false, bool? withpayloads = false)
        {
            var ret = new List<string>();
            var args = item.GetType().SerializeGetSuggestions(prefix, fuzzy, max, withscores, withpayloads);
            var res = connection.Execute("FT.SUGGET", args).ToArray();
            for (var i = 0; i < res.Length; i++)
            {
                ret.Add(res[i]);
            }

            return ret;
        }

        /// <summary>
        /// Add suggestions for the given string.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="item">the type to use for creating the index.</param>
        /// <param name="value">is suggestion string to index.</param>
        /// <param name="score">is floating point number of the suggestion string's weight.</param>
        /// <param name="increment">increment score value.</param>
        /// <param name="payload">jsonpayload.</param>
        /// <returns>A type return long.</returns>
        public static RedisReply SuggestionAdd(this IRedisConnection connection, object item, string value, float score, bool increment = false, object? payload = null)
        {
            var args = item.GetType().SerializeSuggestions(value, score, increment, payload);
            return connection.Execute("FT.SUGADD", args);
        }

        /// <summary>
        /// Delete suggestion with given suggestion string.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="key">is suggestion dictionary key.</param>
        /// <param name="suggestionstring">suggestion string to index.</param>
        /// <returns>if the string was found and deleted.</returns>
        public static bool SuggestionDelete(this IRedisConnection connection, string key, string suggestionstring)
        {
            var args = new[] { key, suggestionstring };
            var result = connection.Execute("FT.SUGDEL", args);
            if (result == 0)
            {
                throw new RedisIndexingException("Given SuggestionString is added to suggestion index");
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Get suggestions length added in redis.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="key">is suggestion dictionary key.</param>
        /// <returns>length of the given key which is added to suggestions.</returns>
        public static long SuggestionStringLength(this IRedisConnection connection, string key)
        {
            var args = new[] { key };
            return connection.Execute("FT.SUGLEN", args);
        }

        /// <summary>
        /// Search redis with the given query.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="query">the query to use in the search.</param>
        /// <returns>a Redis reply.</returns>
        internal static RedisReply SearchRawResult(this IRedisConnection connection, RedisQuery query)
        {
            var args = query.SerializeQuery();
            return connection.Execute("FT.SEARCH", args);
        }

        /// <summary>
        /// Search redis with the given query.
        /// </summary>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="query">the query to use in the search.</param>
        /// <returns>a Redis reply.</returns>
        internal static Task<RedisReply> SearchRawResultAsync(this IRedisConnection connection, RedisQuery query)
        {
            var args = query.SerializeQuery();
            return connection.ExecuteAsync("FT.SEARCH", args);
        }
    }
}
