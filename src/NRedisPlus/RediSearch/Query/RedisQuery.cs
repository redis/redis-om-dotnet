using System;
using System.Collections.Generic;

namespace NRedisPlus.RediSearch

{
    public class RedisQuery
    {
        public long Flags { get; set; }
        public string Index { get; set; } = string.Empty;
        public string QueryText { get; set; } = string.Empty;
        public SearchLimit? Limit { get; set; }
        public RedisFilter? Filter { get; set; }
        public RedisGeoFilter? GeoFilter { get; set; }
        public ReturnFields? Return { get; set; }
        public RedisSortBy? SortBy { get; set; }
        public string[] SerializeQuery()
        {
            var ret = new List<string>();
            if (string.IsNullOrEmpty(Index))
            {
                throw new ArgumentException("Index cannot be null");
            }                
            ret.Add(Index);
            if (string.IsNullOrEmpty(QueryText))
            {
                throw new ArgumentException("Query cannot be null");
            }
            ret.Add(QueryText);
            foreach(var flag in (QueryFlags[])Enum.GetValues(typeof(QueryFlags)))
            {
                if((Flags & (long)flag) == (long)flag)
                {
                    ret.Add(flag.ToString());
                }
            }
            if(Limit != null)
            {
                ret.AddRange(Limit.QueryText);
            }
            if (Filter != null)
            {
                ret.AddRange(Filter.QueryText);
            }
            if(Return!= null)
            {
                ret.AddRange(Return.QueryText);
            }
            if(GeoFilter != null)
            {
                ret.AddRange(GeoFilter.QueryText);
            }
            return ret.ToArray();
        }
    }
}
