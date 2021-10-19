using System.Collections.Generic;

namespace NRedisPlus.RediSearch.AggregationPredicates
{
    /// <summary>
    /// A reduction that takes no arguments.
    /// </summary>
    public class ZeroArgumentReduction : Reduction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZeroArgumentReduction"/> class.
        /// </summary>
        /// <param name="function">the reduction function.</param>
        public ZeroArgumentReduction(ReduceFunction function)
            : base(function)
        {
        }

        /// <inheritdoc/>
        public override string ResultName => $"{Function}";

        /// <inheritdoc/>
        public override IEnumerable<string> Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(Function.ToString());
            ret.Add("0");
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
