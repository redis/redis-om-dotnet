using System.Collections.Generic;
using System.Linq.Expressions;

namespace Redis.OM.RediSearch.AggregationPredicates
{
    /// <summary>
    /// A reduction with two arguments.
    /// </summary>
    public class TwoArgumentReduction : Reduction
    {
        private readonly string _arg1;
        private readonly string _arg2;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoArgumentReduction"/> class.
        /// </summary>
        /// <param name="func">The reduction function.</param>
        /// <param name="expression">The expression.</param>
        public TwoArgumentReduction(ReduceFunction func, MethodCallExpression expression)
            : base(func)
        {
            _arg1 = ExpressionParserUtilities.GetOperandString(expression.Arguments[1]);
            _arg2 = ExpressionParserUtilities.GetOperandString(expression.Arguments[2]);
        }

        /// <summary>
        /// Gets the name of the result.
        /// </summary>
        public override string ResultName => $"{_arg1.Substring(1)}_{Function}_{_arg2}";

        /// <summary>
        /// Sends the reduction to an array of strings for redis.
        /// </summary>
        /// <returns>an array of strings.</returns>
        public override IEnumerable<string> Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(Function.ToString());
            ret.Add("2");
            ret.Add(_arg1);
            ret.Add(_arg2);
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
