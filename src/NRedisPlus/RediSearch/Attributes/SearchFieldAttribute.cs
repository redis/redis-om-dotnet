using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NRedisPlus.RediSearch
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class SearchFieldAttribute : RedisFieldAttribute
    {
        public abstract SearchFieldType SearchFieldType { get; }
        public bool Sortable { get; set; } = false;
        public bool Aggregatable { get; set; } = false;
        public bool Normalize { get; set; } = true;        
    }
}
