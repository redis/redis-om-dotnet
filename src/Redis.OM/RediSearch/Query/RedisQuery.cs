using System;
using System.Collections.Generic;

namespace Redis.OM.RediSearch.Query
{
    /// <summary>
    /// A query to redis.
    /// </summary>
    public sealed class RedisQuery
    {
        /// <summary>
        /// Gets or sets the flags for the query options.
        /// </summary>
        public long Flags { get; set; } = 0;

        /// <summary>
        /// Gets or sets the index to query.
        /// </summary>
        public string Index { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the query text.
        /// </summary>
        public string QueryText { get; set; } = "*";

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public SearchLimit? Limit { get; set; }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        public RedisFilter? Filter { get; set; }

        /// <summary>
        /// gets or sets the geofilter.
        /// </summary>
        public RedisGeoFilter? GeoFilter { get; set; }

        /// <summary>
        /// gets or sets the items to return.
        /// </summary>
        public ReturnFields? Return { get; set; }

        /// <summary>
        /// gets or sets the items to sort by.
        /// </summary>
        public RedisSortBy? SortBy { get; set; }

        /// <summary>
        /// Serializes the query into a set of arguments.
        /// </summary>
        /// <returns>the serialized arguments.</returns>
        /// <exception cref="ArgumentException">thrown if the index is null.</exception>
        internal string[] SerializeQuery()
        {
            var ret = new List<string>();
            if (string.IsNullOrEmpty(Index))
            {
                throw new ArgumentException("Index cannot be null");
            }

            ret.Add(Index);
            ret.Add(QueryText);
            foreach (var flag in (QueryFlags[])Enum.GetValues(typeof(QueryFlags)))
            {
                if ((Flags & (long)flag) == (long)flag)
                {
                    ret.Add(flag.ToString());
                }
            }

            if (Limit != null)
            {
                ret.AddRange(Limit.SerializeArgs);
            }

            if (Filter != null)
            {
                ret.AddRange(Filter.SerializeArgs);
            }

            if (Return != null)
            {
                ret.AddRange(Return.SerializeArgs);
            }

            if (GeoFilter != null)
            {
                ret.AddRange(GeoFilter.SerializeArgs);
            }

            if (SortBy != null)
            {
                ret.AddRange(SortBy.SerializeArgs);
            }

            return ret.ToArray();
        }
    }
}
