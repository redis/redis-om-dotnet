using System;

namespace Redis.OM.RediSearch
{
    /// <summary>
    /// An attribute representing a particular field to be stored in redis.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisFieldAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the property's name in redis.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;
    }
}
