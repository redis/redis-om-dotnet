using System.Collections.Generic;

namespace Redis.OM.RediSearch.AggregationPredicates
{
    /// <summary>
    /// A reduction with one argument.
    /// </summary>
    public class SingleArgumentReduction : Reduction
    {
        private readonly string _arg;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleArgumentReduction"/> class.
        /// </summary>
        /// <param name="function">The reduction function.</param>
        /// <param name="arg">The name of the argument.</param>
        public SingleArgumentReduction(ReduceFunction function, string arg)
            : base(function)
        {
            _arg = arg;
        }

        /// <summary>
        /// Gets the name of the result.
        /// </summary>
        public override string ResultName => $"{_arg}_{Function}";

        /// <inheritdoc/>
        public override IEnumerable<string> Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(Function.ToString());
            ret.Add("1");
            ret.Add($"@{_arg}");
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
