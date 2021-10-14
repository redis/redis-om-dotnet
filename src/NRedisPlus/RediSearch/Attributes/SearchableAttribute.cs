using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SearchableAttribute : SearchFieldAttribute
    {
        public override SearchFieldType SearchFieldType => SearchFieldType.TEXT;
        public bool NoStem { get; set; } = false;
        public string PhoneticMatcher { get; set; } = string.Empty;
        public double Weight { get; set; } = 1;
    }
}
