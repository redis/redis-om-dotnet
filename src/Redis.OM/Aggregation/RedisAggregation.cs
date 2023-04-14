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
        public List<QueryPredicate> Queries { get; set; } = new();
        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public LimitPredicate? Limit { get; set; }

        /// <summary>
        /// Gets the predicates to use for the aggregation.
        /// </summary>
        public Stack<IAggregationPredicate> Predicates { get; } = new ();

        /// <summary>
        /// serializes the aggregation into an array of arguments for redis.
        /// </summary>
        /// <returns>The serialized arguments.</returns>
        public string[] Serialize()
        {
            var queries = new List<string>();
            var ret = new List<string>() { IndexName };
            if (Queries.Any())
            {
                foreach (var query in Queries)
                {
                    queries.AddRange(query.Serialize());
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

            return ret.ToArray();
        }
    }
}
