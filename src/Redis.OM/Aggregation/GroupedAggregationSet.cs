using System.Linq.Expressions;

namespace Redis.OM.Aggregation
{
    /// <summary>
    /// An aggregation set that represents a grouped set of items.
    /// </summary>
    /// <typeparam name="T">The type being aggregated.</typeparam>
    public class GroupedAggregationSet<T> : RedisAggregationSet<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupedAggregationSet{T}"/> class.
        /// </summary>
        /// <param name="source">The previous aggregation set.</param>
        /// <param name="expression">the expression.</param>
        internal GroupedAggregationSet(RedisAggregationSet<T> source, Expression expression)
            : base(source, expression)
        {
        }
    }
}
