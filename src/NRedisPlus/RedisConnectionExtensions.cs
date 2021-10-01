namespace NRedisPlus
{
    public static class RedisConnectionExtensions
    {
        public static IRedisConnection Connect(this RedisConnectionConnfiguration conf) => new RedisConnection(conf);
    }
}