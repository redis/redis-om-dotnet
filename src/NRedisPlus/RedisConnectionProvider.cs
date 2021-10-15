using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using NRedisPlus;
using NRedisPlus.Contracts;
using NRedisPlus.RediSearch;

namespace NRedisPlus
{
    public class RedisConnectionProvider
    {
        public IRedisConnection Connection => new RedisConnection(_Mux.GetDatabase());
        private ConnectionMultiplexer _Mux;
        public RedisConnectionProvider(string connectionString)
        {
            var options = RedisUriParser.ParseConfigFromUri(connectionString);
            _Mux = ConnectionMultiplexer.Connect(options);
        }

        public RedisConnectionProvider(RedisConnectionConfiguration connnection)
        {
            _Mux = ConnectionMultiplexer.Connect(connnection.ToStackExchangeConnectionString());
        }
        
        public RedisAggregationSet<T> AggregationSet<T> () => new RedisAggregationSet<T>(Connection);
        public RedisCollection<T> RedisCollection<T>() where T: notnull => new RedisCollection<T>(Connection);

        
    }
}
