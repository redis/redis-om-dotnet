using System;

namespace NRedisPlus.RediSearch
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisFieldAttribute : Attribute
    {
        public string PropertyName { get; set; } = string.Empty;

    }
}
