using System.Collections.Generic;
using NRedisPlus.Model;

namespace NRedisPlus.RediSearch.AggregationPredicates
{
    /// <summary>
    /// Sort by predicate for an aggregation.
    /// </summary>
    public class AggregateSortBy : IAggregationPredicate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateSortBy"/> class.
        /// </summary>
        /// <param name="property">property to sort by.</param>
        /// <param name="direction">direction to sort by.</param>
        /// <param name="max">maximum number of records to pull.</param>
        public AggregateSortBy(string property, SortDirection direction, int? max = null)
        {
            Property = property;
            Direction = direction;
            Max = max;
        }

        /// <summary>
        /// Gets or sets property to sort by.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets direction to sort.
        /// </summary>
        public SortDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets maximum number of elements.
        /// </summary>
        public int? Max { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> Serialize()
        {
            var numArgs = Max.HasValue ? 4 : 2;
            var ret = new List<string> { "SORTBY", numArgs.ToString(), $"@{Property}", Direction == SortDirection.Ascending ? "ASC" : "DESC" };
            if (Max.HasValue)
            {
                ret.Add("MAX");
                ret.Add(Max.ToString());
            }

            return ret.ToArray();
        }
    }
}
