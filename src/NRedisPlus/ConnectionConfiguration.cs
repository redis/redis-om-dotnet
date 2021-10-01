using System;

namespace NRedisPlus
{
    public class RedisConnectionConnfiguration
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 6379;
        public string? Password { get; set; }
        public string ToStackExchangeConnectionString() => $"{Host}:{Port},password{Password}";
    }
}