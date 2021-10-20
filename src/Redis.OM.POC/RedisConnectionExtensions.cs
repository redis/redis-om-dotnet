using Redis.OM.Contracts;
using StackExchange.Redis;

namespace Redis.OM
{
    public static class RedisConnectionExtensions
    {
        public static IRedisConnection Connect(this RedisConnectionConfiguration conf)
        {
            return new RedisConnection(conf.Host);
        }
    }
}