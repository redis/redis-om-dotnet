using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public static class RediSearchCommands
    {
        public static SearchResponse Search(this IRedisConnection connection, RedisQuery query)
        {
            var res = connection.Execute("FT.SEARCH", query.SerializeQuery());
            return new SearchResponse(res);
        }

        public static async Task<SearchResponse> SearchAsync(this IRedisConnection connection, RedisQuery query)
        {
            var res = await connection.ExecuteAsync("FT.SEARCH", query.SerializeQuery());
            return new SearchResponse(res);
        }

        public static RedisReply Search<T>(this IRedisConnection connection, RedisQuery query)
            where T : notnull
        {
            var args = query.SerializeQuery();
            return connection.Execute("FT.SEARCH", args);            
        }        

        public static async Task<SearchResponse<T>> SearchAsync<T>(this IRedisConnection connection, RedisQuery query)
            where T : notnull
        {
            var res = await connection.ExecuteAsync("FT.SEARCH", query.SerializeQuery());
            return new SearchResponse<T>(res);
        }

        public static bool CreateIndex(this IRedisConnection connection, Type type)
        {
            var serializedParams = type.SerializeIndex();
            connection.Execute("FT.CREATE", serializedParams);
            return true;
        }

        public static async Task<bool> CreateIndexAsync(this IRedisConnection connection, Type type)
        {
            var serializedParams = type.SerializeIndex();
            await connection.ExecuteAsync("FT.CREATE", serializedParams);
            return true;
        }

        public static async Task<bool> DropIndexAsync(this IRedisConnection connection, Type type)
        {
            var indexName = type.SerializeIndex().FirstOrDefault();
            await connection.ExecuteAsync("FT.DROPINDEX", indexName);
            return true;
        }

        public static bool DropIndex(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().FirstOrDefault();
                connection.Execute("FT.DROPINDEX", indexName);
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                    return false;
                throw;
            }
        }

        public static bool DropIndexAndAssociatedRecords(this IRedisConnection connection, Type type)
        {
            try
            {
                var indexName = type.SerializeIndex().FirstOrDefault();
                connection.Execute("FT.DROPINDEX", indexName, "DD");
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unknown Index name"))
                    return false;
                throw;
            }
        }

    }
}