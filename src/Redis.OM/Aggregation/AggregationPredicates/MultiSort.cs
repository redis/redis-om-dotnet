using System.Collections.Generic;
using System.Linq;

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
            var numArgs = _subPredicates.Sum(x => x.NumArgs);
            List<string> args = new List<string>(numArgs) { "SORTBY", numArgs.ToString() };
            foreach (var predicate in _subPredicates)
            {
                args.AddRange(predicate.Serialize().Skip(2));
            }

            return args;
        }
    }
}