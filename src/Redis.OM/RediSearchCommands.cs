using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;

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
            var serializedParams = type.SerializeIndex();
            connection.Execute("FT.CREATE", serializedParams);
            return true;
        }

        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to use for creating the index.</param>
        /// <returns>whether the index was created or not.</returns>
        public static async Task<bool> CreateIndexAsync(this IRedisConnection connection, Type type)
        {
            var serializedParams = type.SerializeIndex();
            await connection.ExecuteAsync("FT.CREATE", serializedParams);
            return true;
        }

        /// <summary>
        /// Deletes an index.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="type">the type to drop the index for.</param>
        /// <returns>whether the index was dropped or not.</returns>
        public static async Task<bool> DropIndexAsync(this IRedisConnection connection, Type type)
        {
            var indexName = type.SerializeIndex().First();
            await connection.ExecuteAsync("FT.DROPINDEX", indexName);
            return true;
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
        /// Gets the Index with the Index Name Pattern.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <param name="indexName">Name of the Index to be queries.</param>
        /// <returns>Information about the Info.</returns>
        public static RedisIndexInfo GetIndexInfo(this IRedisConnection connection, string indexName)
        {
            var reply = connection.Execute("FT.INFO", indexName);
            var res = reply.ToArray();
            var info = ToRedisInfo(res);

            return info;
        }

        /// <summary>
        /// Gets the Index with the Index Name Pattern.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <param name="indexName">Name of the Index to be queries.</param>
        /// <returns>Information about the Info.</returns>
        public static async Task<RedisIndexInfo> GetIndexInfoasync(
            this IRedisConnection connection,
            string indexName)
        {
            var reply = await connection.ExecuteAsync("FT.INFO", indexName);
            var res = reply.ToArray();
            var info = ToRedisInfo(res);
            return info;
        }

        /// <summary>
        /// Get The Index based on Type.
        /// </summary>
        /// <param name="connection">Connection Object.</param>
        /// <typeparam name="T">Type of object from which the index name is derived.</typeparam>
        /// <returns>Dictionary of Info.</returns>
        public static RedisIndexInfo GetIndexInfo<T>(this IRedisConnection connection)
        {
            var indexName = typeof(T).SerializeIndex().First();
            return connection.GetIndexInfo(indexName);
        }

        /// <summary>
        /// Get The Index based on Type.
        /// </summary>
        /// <param name="connection">Connection Object.</param>
        /// <typeparam name="T">Type of object from which the index name is derived.</typeparam>
        /// <returns>Dictionary of Info.</returns>
        public static async Task<RedisIndexInfo> GetIndexInfoasync<T>(this IRedisConnection connection)
        {
            var indexName = typeof(T).SerializeIndex().First();
            return await connection.GetIndexInfoasync(indexName);
        }

        /// <summary>
        /// Converts the Redis Reply List to Dictionary.
        /// </summary>
        /// <param name="res">Response from the command.</param>
        /// <returns>Dictionary of Key and Value.</returns>
        internal static RedisIndexInfo ToRedisInfo(RedisReply[] res)
        {
            RedisIndexInfo info = new RedisIndexInfo();
            info.IndexName = res[1];
            return info;
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
    }
}