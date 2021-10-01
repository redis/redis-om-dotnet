using System;

namespace NRedisPlus
{
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
    public class RedisHashClass:Attribute
    {
        public RedisHashClass(string prefix = "", bool autogenerateIds = true)
        {
        }
    }
}
