using System.Collections.Generic;
using System.Linq.Expressions;

namespace Redis.OM.Common
{
    /// <summary>
    /// A boolean expression.
    /// </summary>
    public abstract class BooleanExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanExpression"/> class.
        /// </summary>
        /// <param name="expression">the expression.</param>
        internal BooleanExpression(LambdaExpression expression)
        {
            Expression = expression;
        }

        /// <summary>
        /// Gets or sets the Lambda expression.
        /// </summary>
        protected LambdaExpression Expression { get; set; }

        /// <summary>
        /// Validates an operand and pushes it onto the stack.
        /// </summary>
        /// <param name="expression">the expression.</param>
        /// <param name="stack">the stack.</param>
        protected abstract void ValidateAndPushOperand(Expression expression, Stack<string> stack);

        /// <summary>
        /// Splits the binary expression into a usable query.
        /// </summary>
        /// <param name="expression">the expression.</param>
        /// <param name="stack">the operand stack.</param>
        protected abstract void SplitBinaryExpression(BinaryExpression expression, Stack<string> stack);

        /// <summary>
        /// splits the expression recursively.
        /// </summary>
        /// <returns>a stack of predicate strings.</returns>
        protected Stack<string> SplitExpression()
        {
            var ret = new Stack<string>();
            if (Expression.Body is BinaryExpression binExpression)
            {
                SplitBinaryExpression(binExpression, ret);
            }
            else
            {
                ValidateAndPushOperand(Expression.Body, ret);
            }

            return ret;
        }
    }
}
