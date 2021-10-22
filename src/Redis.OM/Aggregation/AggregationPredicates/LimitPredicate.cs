using System.Collections.Generic;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// A predicate for limiting results of an aggregation.
    /// </summary>
    public class LimitPredicate : IAggregationPredicate
    {
        /// <summary>
        /// Gets or sets the offset to use to step into the results.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Gets or sets the number of items to return.
        /// </summary>
        public long Count { get; set; } = 100;

        /// <inheritdoc/>
        public IEnumerable<string> Serialize()
        {
            return new[] { "LIMIT", Offset.ToString(), Count.ToString() };
        }
    }
}
