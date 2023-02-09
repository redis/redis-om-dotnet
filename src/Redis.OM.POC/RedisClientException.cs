using System;

namespace Redis.OM
{
    public class RedisClientException : Exception
    {
        public RedisClientException(string message) : base(message) { }
    }
}
