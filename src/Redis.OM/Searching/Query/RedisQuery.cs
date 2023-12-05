using System;
using System.Collections.Generic;
using System.Linq;
using Redis.OM.Modeling.Vectors;

namespace Redis.OM.Searching.Query
{
    /// <summary>
    /// A query to redis.
    /// </summary>
    public sealed class RedisQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQuery"/> class.
        /// An object to facilitate the Raw Redis query.
        /// </summary>
        /// <param name="index">Name of the Index to query.</param>
        public RedisQuery(string index)
        {
            this.Index = index;
        }

        /// <summary>
        /// Gets or sets the nearest neighbors query.
        /// </summary>
        public NearestNeighbors? NearestNeighbors { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the query.
        /// </summary>
        public List<object> Parameters { get; set; } = new ();

        /// <summary>
        /// Gets or sets the flags for the query options.
        /// </summary>
        public long Flags { get; set; } = 0;

        /// <summary>
        /// Gets or sets the index to query.
        /// </summary>
        public string Index { get; set; }

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
        internal object[] SerializeQuery()
        {
            var parameters = new List<object>(Parameters);
            var ret = new List<object>();
            if (string.IsNullOrEmpty(Index))
            {
                throw new ArgumentException("Index cannot be null");
            }

            ret.Add(Index);

            if (NearestNeighbors is not null)
            {
                var queryText = $"({QueryText})=>[KNN {NearestNeighbors.NumNeighbors} @{NearestNeighbors.PropertyName} ${parameters.Count} AS {VectorScores.NearestNeighborScoreName}]";
                ret.Add(queryText);
                parameters.Add(NearestNeighbors.VectorBlob);
            }
            else
            {
                ret.Add(QueryText);
            }

            if (parameters.Any())
            {
                ret.Add("PARAMS");
                ret.Add(parameters.Count * 2);
                for (var i = 0; i < parameters.Count; i++)
                {
                    ret.Add(i.ToString());
                    ret.Add(parameters[i]);
                }

                ret.Add("DIALECT");
                ret.Add(2);
            }

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
