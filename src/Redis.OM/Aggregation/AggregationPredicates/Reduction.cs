using System.Collections.Generic;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// A reduction.
    /// </summary>
    public abstract class Reduction : IAggregationPredicate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Reduction"/> class.
        /// </summary>
        /// <param name="function">The function to reduce to.</param>
        protected Reduction(ReduceFunction function)
        {
            Function = function;
        }

        /// <summary>
        /// Gets the alias of the result name when the reduction completes.
        /// </summary>
        public abstract string ResultName { get; }

        /// <summary>
        /// Gets The function to use for reduction.
        /// </summary>
        protected ReduceFunction Function { get; }

        /// <inheritdoc/>
        public abstract IEnumerable<string> Serialize();
    }
}
