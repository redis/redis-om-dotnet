using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class AggregateSortBy : IAggregationPredicate
    {
        public string Property { get; set; }
        public SortDirection Direction { get; set; }
        public int? Max { get; set; }

        public AggregateSortBy(string property, SortDirection direction, int? max = null)
        {
            Property = property;
            Direction = direction;
            Max = max;
        }

        public string[] Serialize()
        {
            var numArgs = Max.HasValue ? 4 : 2;
            var ret = new List<string> { "SORTBY", numArgs.ToString(), $"@{Property}", Direction == SortDirection.Ascending ? "ASC" : "DESC"};            
            if (Max.HasValue)
            {
                ret.Add("MAX");
                ret.Add(Max.ToString());
            }
                
            return ret.ToArray();
        }
    }
}
