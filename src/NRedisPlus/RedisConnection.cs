//using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NRedisPlus
{
    internal class RedisConnection : IRedisConnection
    {
        private ConnectionMultiplexer connectionMultiplexer;
        private readonly IDatabase _db;
        
        internal RedisConnection(RedisConnectionConnfiguration conf)
        {
            var seConf = new ConfigurationOptions();
            seConf.EndPoints.Add(conf.Host, conf.Port);
            seConf.Password = conf.Password;
            connectionMultiplexer = ConnectionMultiplexer.Connect(seConf);
            _db = connectionMultiplexer.GetDatabase();
        }

        internal RedisConnection(IDatabase db)
        {
            _db = db;
        }

        public RedisReply Execute(string command, params string[] args)
        {
            var result = _db.Execute(command, args);
            return new RedisReply(result);
        }

        public async Task<RedisReply> ExecuteAsync(string command, params string[] args)
        {
            var result = await _db.ExecuteAsync(command, args);
            return new RedisReply(result);
        }

        public void Dispose()
        {
            if (connectionMultiplexer!=null) connectionMultiplexer.Dispose();
        }
    }
}
