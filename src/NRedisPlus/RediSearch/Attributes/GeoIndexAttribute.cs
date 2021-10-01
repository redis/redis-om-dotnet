using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GeoIndexAttribute : SearchFieldAttribute
    {
        public override SearchFieldType SearchFieldType => SearchFieldType.GEO;
    }
}
