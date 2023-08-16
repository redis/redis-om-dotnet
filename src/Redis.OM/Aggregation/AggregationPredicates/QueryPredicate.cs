using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Redis.OM.Common;
using Redis.OM.Modeling;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// A query predicate for an aggregation.
    /// </summary>
    public class QueryPredicate : BooleanExpression, IAggregationPredicate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryPredicate"/> class.
        /// </summary>
        /// <param name="exp">the expression.</param>
        public QueryPredicate(LambdaExpression exp)
            : base(exp)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryPredicate"/> class.
        /// </summary>
        public QueryPredicate()
            : base(System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant("*")))
        {
        }

        /// <inheritdoc/>
        public IEnumerable<string> Serialize()
        {
            var predicateStack = SplitExpression();
            return new[] { string.Join(" ", predicateStack) };
        }

        /// <inheritdoc/>
        protected override void ValidateAndPushOperand(Expression expression, Stack<string> stack)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                var memberExpression = binaryExpression.Left as MemberExpression;
                if (memberExpression is null)
                {
                    if (binaryExpression.Left is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression } leftUnary)
                    {
                        memberExpression = (MemberExpression)leftUnary.Operand;
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid Expression Type");
                    }
                }

                if (binaryExpression.Right is ConstantExpression constantExpression)
                {
                    stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression, constantExpression));
                }
                else if (binaryExpression.Right is UnaryExpression uni)
                {
                    switch (uni.Operand)
                    {
                        case ConstantExpression c:
                            stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression, c));
                            break;
                        case MemberExpression mem:
                        {
                            var val = ExpressionParserUtilities.GetOperandString(mem);
                            stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression, System.Linq.Expressions.Expression.Constant(val)));
                            break;
                        }
                    }
                }
                else if (binaryExpression.Right is MemberExpression mem)
                {
                    var val = ExpressionParserUtilities.GetOperandString(mem);
                    stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression, System.Linq.Expressions.Expression.Constant(val)));
                }
                else
                {
                    var val = ExpressionParserUtilities.GetOperandStringForQueryArgs(binaryExpression.Right);
                    stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression, System.Linq.Expressions.Expression.Constant(val)));
                }
            }
            else if (expression is ConstantExpression c
                     && c.Value.ToString() == "*")
            {
                stack.Push(c.Value.ToString());
            }
            else if (expression is MethodCallExpression method)
            {
                stack.Push(ExpressionParserUtilities.TranslateMethodExpressions(method));
            }
            else if (expression is UnaryExpression uni)
            {
                var operandStack = new Stack<string>();

                if (uni.Operand is BinaryExpression be)
                {
                    SplitBinaryExpression(be, operandStack);
                }
                else
                {
                    ValidateAndPushOperand(uni.Operand, operandStack);
                }

                var val = string.Join(" ", operandStack);
                if (uni.NodeType == ExpressionType.Not)
                {
                    val = $"(-({val}))";
                }

                stack.Push(val);
            }
            else
            {
                throw new ArgumentException("Invalid Expression Type");
            }
        }

        /// <summary>
        /// Splits the binary expression, and pushes it's predicates onto the stack.
        /// </summary>
        /// <param name="expression">the expression to split.</param>
        /// <param name="stack">the stack.</param>
        protected override void SplitBinaryExpression(BinaryExpression expression, Stack<string> stack)
        {
            var left = expression.Left as BinaryExpression;
            var right = expression.Right as BinaryExpression;

            if (left != null && right != null)
            {
                stack.Push(")");
                SplitBinaryExpression(right, stack);
                if (expression.NodeType == ExpressionType.Or || expression.NodeType == ExpressionType.OrElse)
                {
                    stack.Push((left.Left as MemberExpression)?.Member.Name != (right.Left as MemberExpression)?.Member.Name ? ")|(" : "|");
                }

                SplitBinaryExpression(left, stack);
                stack.Push("(");
            }
            else if (left != null)
            {
                ValidateAndPushOperand(expression.Right, stack);
                if (expression.NodeType == ExpressionType.Or)
                {
                    stack.Push("|");
                }

                SplitBinaryExpression(left, stack);
            }
            else if (right != null)
            {
                SplitBinaryExpression(right, stack);
                if (expression.NodeType == ExpressionType.Or)
                {
                    stack.Push("|");
                }

                ValidateAndPushOperand(expression.Left, stack);
            }
            else
            {
                ValidateAndPushOperand(expression, stack);
            }
        }

        private static string BuildEqualityPredicate(MemberInfo member, ConstantExpression expression, string memberStr, bool negated = false)
        {
            var sb = new StringBuilder();
            var fieldAttribute = member.GetCustomAttribute<SearchFieldAttribute>();
            if (fieldAttribute == null)
            {
                throw new InvalidOperationException("Searches can only be performed on fields marked with a RedisFieldAttribute with the SearchFieldType not set to None");
            }

            if (negated)
            {
                sb.Append("-");
            }

            sb.Append($"{memberStr}:");
            var searchFieldType = fieldAttribute.SearchFieldType != SearchFieldType.INDEXED
                ? fieldAttribute.SearchFieldType
                : ExpressionTranslator.DetermineIndexFieldsType(member);
            switch (searchFieldType)
            {
                case SearchFieldType.TAG:
                    sb.Append($"{{{ExpressionParserUtilities.EscapeTagField(expression.Value.ToString())}}}");
                    break;
                case SearchFieldType.TEXT:
                    sb.Append(expression.Value);
                    break;
                case SearchFieldType.NUMERIC:
                    sb.Append($"[{expression.Value} {expression.Value}]");
                    break;
                default:
                    throw new InvalidOperationException("Could not translate query equality searches only supported for Tag, text, and numeric fields");
            }

            return sb.ToString();
        }

        private string BuildQueryPredicate(ExpressionType expType, MemberExpression member, ConstantExpression constExpression)
        {
            var memberStr = ExpressionParserUtilities.GetOperandString(member);
            var queryPredicate = expType switch
            {
                ExpressionType.GreaterThan => $"{memberStr}:[({constExpression.Value} inf]",
                ExpressionType.LessThan => $"{memberStr}:[-inf ({constExpression.Value}]",
                ExpressionType.GreaterThanOrEqual => $"{memberStr}:[{constExpression.Value} inf]",
                ExpressionType.LessThanOrEqual => $"{memberStr}:[-inf {constExpression.Value}]",
                ExpressionType.Equal => BuildEqualityPredicate(member.Member, constExpression, memberStr),
                ExpressionType.NotEqual => BuildEqualityPredicate(member.Member, constExpression, memberStr, true),
                _ => string.Empty
            };
            return queryPredicate;
        }
    }
}
