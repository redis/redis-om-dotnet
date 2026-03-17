using System;
using System.Collections.Generic;
using System.Linq;
using Redis.OM.Aggregation.AggregationPredicates;

namespace Redis.OM.Aggregation
{
    /// <summary>
    /// An aggregation pipeline.
    /// </summary>
    public class RedisAggregation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisAggregation"/> class.
        /// </summary>
        /// <param name="indexName">The index being queried.</param>
        public RedisAggregation(string indexName)
        {
            IndexName = indexName;
        }

        /// <summary>
        /// Gets the index to run the aggregation on.
        /// </summary>
        public string IndexName { get; }

        /// <summary>
        /// Gets or sets the query predicate.
        /// </summary>
        public QueryPredicate Query { get; set; } = new ();

        /// <summary>
        /// Gets or sets the query predicate.
        /// </summary>
        public List<QueryPredicate> Queries { get; set; } = new ();

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public LimitPredicate? Limit { get; set; }

        /// <summary>
        /// Gets the predicates to use for the aggregation.
        /// </summary>
        public Stack<IAggregationPredicate> Predicates { get; } = new ();

        /// <summary>
        /// Gets or sets the raw query string for direct execution.
        /// </summary>
        public string? RawQuery { get; set; }

        /// <summary>
        /// serializes the aggregation into an array of arguments for redis.
        /// </summary>
        /// <param name="withCursor">Whether to serialize cursor arguments.</param>
        /// <param name="cursorCount">The cursor page size.</param>
        /// <returns>The serialized arguments.</returns>
        public string[] Serialize(bool withCursor = false, int cursorCount = 1000)
        {
            var queries = new List<string>();
            var ret = new List<string>() { IndexName };
            var dialect = 1;
            if (!string.IsNullOrEmpty(RawQuery))
            {
                ret.Add(RawQuery!);
            }
            else if (Queries.Any())
            {
                foreach (var query in Queries)
                {
                    queries.AddRange(query.Serialize());
                    dialect = Math.Max(dialect, query.Dialect);
                }

                ret.AddRange(new[] { string.Join(" ", queries) });
            }
            else
            {
                ret.Add("*");
            }

            foreach (var predicate in Predicates)
            {
                ret.AddRange(predicate.Serialize());
            }

            if (Limit != null)
            {
                ret.AddRange(Limit.Serialize());
            }

            if (withCursor)
            {
                ret.Add("WITHCURSOR");
                ret.Add("COUNT");
                ret.Add(cursorCount.ToString());
            }

            if (dialect > 1)
            {
                ret.Add("DIALECT");
                ret.Add(dialect.ToString());
            }

            return ret.ToArray();
        }
    }
}
