using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class AggregationResult<T> : IAggregationResult
    {
        /// <summary>
        /// Shell of a Record stored on the database, only appropriate to use
        /// inside of aggregation pipelines, no real data will be stored in here once 
        /// the AggregationResult is Hydrated
        /// </summary>
        public T? RecordShell { get; set; } = default(T);
        public IDictionary<string, RedisReply> Aggregations { get; set; }
        public RedisReply this[string key]
        {
            get => Aggregations[key];            
        } 

        public AggregationResult(RedisReply res)
        {
            Aggregations = new Dictionary<string, RedisReply>();
            var arr = res.ToArray();
            for(var i = 0; i < arr.Length; i+=2)
            {
                Aggregations.Add(arr[i], arr[i + 1]);
            }            
        }        

        public static IEnumerable<AggregationResult<T>> FromRedisResult(RedisReply res)
        {
            var arr = res.ToArray();
            var list = new List<AggregationResult<T>>();
            for (var i = 1; i < arr.Length; i++)
            {
                list.Add(new AggregationResult<T>(arr[i]));
            }
            return list;
        }
    }

    public class AggregationResult : IAggregationResult
    {
        public object? Record { get; set; } = default(object);
        public IDictionary<string, RedisReply> Aggregations { get; set; }
        public RedisReply this[string key]
        {
            get => Aggregations[key];
        }

        public AggregationResult(RedisReply res)
        {
            Aggregations = new Dictionary<string, RedisReply>();
            var arr = res.ToArray();
            for (var i = 0; i < arr.Length; i += 2)
            {
                Aggregations.Add(arr[i], arr[i + 1]);
            }
        }

        public static IEnumerable<AggregationResult> FromRedisResult(RedisReply res)
        {
            var arr = res.ToArray();
            var list = new List<AggregationResult>();
            for (var i = 1; i < arr.Length; i++)
            {
                list.Add(new AggregationResult(arr[i]));
            }
            return list;
        }
    }
}
