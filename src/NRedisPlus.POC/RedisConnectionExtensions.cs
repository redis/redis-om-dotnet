using NRedisPlus.Contracts;
using StackExchange.Redis;

namespace NRedisPlus
{
    public static class RedisConnectionExtensions
    {
        public static IRedisConnection Connect(this RedisConnectionConfiguration conf)
        {
            return new RedisConnection(conf.Host);
        }
    }
}