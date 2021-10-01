using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class QueryPredicate : BooleanExpression, IAggregationPredicate
    {
        public void SetLambda(LambdaExpression lambda)
        {
            _expression = lambda;
        }
        public QueryPredicate(LambdaExpression exp) : base(exp) { }
        public QueryPredicate() : base(Expression.Lambda(Expression.Constant("*"))) { }

        private string BuildEqualityPredicate(MemberInfo member, ConstantExpression expression)
        {
            var sb = new StringBuilder();
            var fieldAttribute = member.GetCustomAttribute<SearchFieldAttribute>();
            if (fieldAttribute == null)
            {
                throw new InvalidOperationException("Searches can only be performed on fields marked with a RedisFieldAttribute with the SearchFieldType not set to None");
            }
            sb.Append($"@{member.Name}:");
            var searchFieldType = fieldAttribute.SearchFieldType != SearchFieldType.INDEXED
                ? fieldAttribute.SearchFieldType
                : ExpressionTranslator.DetermineIndexFieldsType(member);
            switch (searchFieldType)
            {
                case SearchFieldType.TAG:
                    sb.Append($"{{{expression.Value}}}");
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

        private string BuildQueryPredicate(ExpressionType expType, MemberInfo member, ConstantExpression constExpression)
        {
            var queryPredicate = expType switch
            {
                ExpressionType.GreaterThan => $"@{member.Name}:[({constExpression.Value} inf]",
                ExpressionType.LessThan => $"@{member.Name}:[-inf ({constExpression.Value}]",
                ExpressionType.GreaterThanOrEqual => $"@{member.Name}:[{constExpression.Value} inf]",
                ExpressionType.LessThanOrEqual => $"@{member.Name}:[-inf {constExpression.Value}]",
                ExpressionType.Equal => BuildEqualityPredicate(member, constExpression),
                ExpressionType.NotEqual => $"@{member.Name} : -{{{constExpression.Value}}}",
                _ => string.Empty
            };
            return queryPredicate;
        }

        protected override string GetOperatorSymbol(ExpressionType expressionType)
        {
            throw new NotImplementedException();
        }

        protected override void ValidateAndPushOperand(Expression expression, Stack<string> stack)
        {
            if (expression is BinaryExpression binaryExpression
                && binaryExpression.Left is MemberExpression memberExpression)
            {
                if (binaryExpression.Right is ConstantExpression constantExpression)
                {
                    stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression.Member, constantExpression));
                }
                else if (binaryExpression.Right is UnaryExpression uni)
                {
                    if(uni.Operand is ConstantExpression c)
                        stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression.Member, c));
                    else if (uni.Operand is MemberExpression mem
                        && mem.Expression is ConstantExpression frame)
                    {
                        var val = ExpressionParserUtilities.GetValue(mem.Member, frame.Value);
                        stack.Push(BuildQueryPredicate(binaryExpression.NodeType, memberExpression.Member, Expression.Constant(val)));
                    }
                }
            }
            else if (expression is ConstantExpression c
                     && c.Value.ToString() == "*")
            {
                stack.Push(c.Value.ToString());
            }
            else
            {
                throw new ArgumentException("Invalid Expression Type");
            }
        }

        protected override void SplitBinaryExpression(BinaryExpression expression, Stack<string> stack)
        {            
            if(expression.Left is BinaryExpression left)
            {
                SplitBinaryExpression(left, stack);
                ValidateAndPushOperand(expression.Right, stack);
            }
            else
            {
                ValidateAndPushOperand(expression, stack);
            }
        }

        public string[] Serialize()
        {
            var predicateStack = SplitExpression();
            return new[] { string.Join(" ", predicateStack) };
        }
    }
}
