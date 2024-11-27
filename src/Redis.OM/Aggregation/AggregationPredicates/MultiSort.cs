using System.Collections.Generic;
using System.Linq;
using Redis.OM.Searching;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// Allows a grouping together of multiple sortby predicates.
    /// </summary>
    public class MultiSort : IAggregationPredicate
    {
        private readonly Stack<AggregateSortBy> _subPredicates = new Stack<AggregateSortBy>();

        /// <summary>
        /// Inserts a predicate into the multi-sort.
        /// </summary>
        /// <param name="sb">The sortby predicate.</param>
        public void InsertPredicate(AggregateSortBy sb)
        {
            _subPredicates.Push(sb);
        }

        /// <inheritdoc />
        public IEnumerable<string> Serialize()
        {
            var numArgs = _subPredicates.Sum(x => AggregateSortBy.NumArgs);
            var max = _subPredicates.FirstOrDefault(x => x.Max.HasValue)?.Max;
            var args = new List<string>(numArgs) { "SORTBY", numArgs.ToString() };
            foreach (var predicate in _subPredicates)
            {
                args.Add($"@{predicate.Property}");
                args.Add(predicate.Direction == SortDirection.Ascending ? "ASC" : "DESC");
            }

            if (max.HasValue)
            {
                args.Add("MAX");
                args.Add(max.ToString());
            }

            return args;
        }
    }
}