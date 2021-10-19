using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NRedisPlus.Model;

namespace NRedisPlus.RediSearch.AggregationPredicates
{
    /// <summary>
    /// Get's the first value of a group matching the expression.
    /// </summary>
    public class FirstValueReduction : Reduction
    {
        private readonly string _returnArg;
        private readonly int _numArgs = 1;
        private readonly string _sortArg = string.Empty;
        private readonly SortDirection? _direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstValueReduction"/> class.
        /// </summary>
        /// <param name="exp">The expression.</param>
        public FirstValueReduction(MethodCallExpression exp)
            : base(ReduceFunction.FIRST_VALUE)
        {
            _returnArg = ExpressionParserUtilities.GetOperandString(exp.Arguments[1]);
            if (exp.Arguments.Count > 2)
            {
                _sortArg = ExpressionParserUtilities.GetOperandString(exp.Arguments[2]);
                _numArgs += 2;
            }

            if (exp.Arguments.Count <= 3)
            {
                return;
            }

            var dir = ExpressionParserUtilities.GetOperandString(exp.Arguments[3]);
            if (!Enum.TryParse(dir, out SortDirection enumeratedDir))
            {
                return;
            }

            _direction = enumeratedDir;
            _numArgs++;
        }

        /// <inheritdoc/>
        public override string ResultName => $"{_returnArg.Substring(1)}_{Function}";

        /// <inheritdoc/>
        public override IEnumerable<string> Serialize()
        {
            var ret = new List<string>
            {
                "REDUCE",
                Function.ToString(),
                _numArgs.ToString(),
                _returnArg,
            };
            if (!string.IsNullOrEmpty(_sortArg))
            {
                ret.Add("BY");
                ret.Add($"@{_sortArg}");

                if (_direction != null)
                {
                    ret.Add(_direction == SortDirection.Ascending ? "ASC" : "DESC");
                }
            }

            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
