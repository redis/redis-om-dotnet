using System.Collections.Generic;
using System.Linq.Expressions;
using Redis.OM.Common;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// A predicate building a function to apply to items in the aggregation pipeline.
    /// </summary>
    public class Apply : IAggregationPredicate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Apply"/> class.
        /// </summary>
        /// <param name="expression">the expression.</param>
        /// <param name="alias">The alias.</param>
        public Apply(Expression expression, string alias)
        {
            Expression = expression;
            Alias = alias;
        }

        /// <summary>
        /// Gets or sets the alias.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// gets the expression.
        /// </summary>
        protected Expression Expression { get; }

        /// <inheritdoc/>
        public IEnumerable<string> Serialize()
        {
            var list = new List<string>();
            list.Add("APPLY");
            switch (Expression)
            {
                case BinaryExpression rootBinExpression:
                    list.Add(ExpressionParserUtilities.ParseBinaryExpression(rootBinExpression));
                    break;
                case MethodCallExpression method:
                    list.Add(ExpressionParserUtilities.GetOperandString(method));
                    break;
                default:
                    list.Add(ExpressionParserUtilities.GetOperandString(Expression));
                    break;
            }

            list.Add("AS");
            list.Add(Alias);
            return list.ToArray();
        }
    }
}
