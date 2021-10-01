using System;

namespace NRedisPlus.RediSearch
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisFieldAttribute : Attribute
    {
        public string PropertyName { get; private set; }

        public RedisFieldAttribute(string propertyName = "")
        {
            PropertyName = propertyName;            
        }
    }
}
