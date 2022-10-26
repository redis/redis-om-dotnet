using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;

namespace Redis.OM.Common
{
    /// <summary>
    /// Translates expressions into usable queries and aggregations.
    /// </summary>
    internal class ExpressionTranslator
    {
        /// <summary>
        /// Build's an aggregation from an expression.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <param name="type">The type indexed by the expression.</param>
        /// <returns>An aggregation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if enclosing type is not indexed.</exception>
        public static RedisAggregation BuildAggregationFromExpression(Expression expression, Type type)
        {
            var attr = type.GetCustomAttribute<DocumentAttribute>();
            if (attr == null)
            {
                throw new InvalidOperationException("Aggregations can only be perfomred on objects decorated with a RedisObjectDefinitionAttribute that specifies a particular index");
            }

            var indexName = string.IsNullOrEmpty(attr.IndexName) ? $"{type.Name.ToLower()}-idx" : attr.IndexName;
            var aggregation = new RedisAggregation(indexName!);
            if (expression is not MethodCallExpression methodExpression)
            {
                return aggregation;
            }

            var expressions = new List<MethodCallExpression> { methodExpression };
            while (methodExpression.Arguments[0] is MethodCallExpression innerExpression)
            {
                expressions.Add(innerExpression);
                methodExpression = innerExpression;
            }

            for (var i = 0; i < expressions.Count; i++)
            {
                var exp = expressions[i];
                LambdaExpression lambda;
                switch (exp.Method.Name)
                {
                    case "FirstOrDefault":
                    case "First":
                    case "FirstOrDefaultAsync":
                    case "FirstAsync":
                        aggregation.Limit = new LimitPredicate { Count = 1 };
                        break;
                    case "Where":
                        lambda = (LambdaExpression)((UnaryExpression)exp.Arguments[1]).Operand;
                        if (i == expressions.Count - 1)
                        {
                            aggregation.Query = new QueryPredicate(lambda);
                        }
                        else
                        {
                            aggregation.Predicates.Push(new FilterPredicate(lambda.Body));
                        }

                        break;
                    case "Average":
                    case "AverageAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.AVG, aggregation.Predicates);
                        break;
                    case "StandardDeviation":
                    case "StandardDeviationAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.STDDEV, aggregation.Predicates);
                        break;
                    case "Sum":
                    case "SumAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.SUM, aggregation.Predicates);
                        break;
                    case "Min":
                    case "MinAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.MIN, aggregation.Predicates);
                        break;
                    case "Max":
                    case "MaxAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.MAX, aggregation.Predicates);
                        break;
                    case "OrderBy":
                        PushAggregateSortBy(exp, SortDirection.Ascending, aggregation.Predicates);
                        break;
                    case "OrderByDescending":
                        PushAggregateSortBy(exp, SortDirection.Descending, aggregation.Predicates);
                        break;
                    case "Take":
                        if (aggregation.Limit != null)
                        {
                            aggregation.Limit.Count = TranslateTake(exp);
                        }
                        else
                        {
                            aggregation.Limit = new LimitPredicate { Count = TranslateTake(exp) };
                        }

                        break;
                    case "Skip":
                        if (aggregation.Limit != null)
                        {
                            aggregation.Limit.Count = TranslateSkip(exp);
                        }
                        else
                        {
                            aggregation.Limit = new LimitPredicate { Offset = TranslateSkip(exp) };
                        }

                        break;
                    case "Count":
                    case "LongCount":
                    case "CountAsync":
                    case "LongCountAsync":
                        TranslateAndPushZeroArgumentPredicate(ReduceFunction.COUNT, aggregation.Predicates);
                        break;
                    case "CountGroupMembers":
                        TranslateAndPushZeroArgumentPredicate(ReduceFunction.COUNT, aggregation.Predicates);
                        break;
                    case "CountDistinct":
                    case "CountDistinctAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.COUNT_DISTINCT, aggregation.Predicates);
                        break;
                    case "CountDistinctish":
                    case "CountDistinctishAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.COUNT_DISTINCTISH, aggregation.Predicates);
                        break;
                    case "GroupBy":
                        TranslateAndPushGroupBy(aggregation.Predicates, exp);
                        break;
                    case "Apply":
                        aggregation.Predicates.Push(TranslateApplyPredicate(exp));
                        break;
                    case "Filter":
                        lambda = (LambdaExpression)((UnaryExpression)exp.Arguments[1]).Operand;
                        aggregation.Predicates.Push(new FilterPredicate(lambda.Body));
                        break;
                    case "Quantile":
                    case "QuantileAsync":
                        TranslateAndPushTwoArgumentReductionPredicate(exp, ReduceFunction.QUANTILE, aggregation.Predicates);
                        break;
                    case "Distinct":
                    case "DistinctAsync":
                        TranslateAndPushReductionPredicate(exp, ReduceFunction.TOLIST, aggregation.Predicates);
                        break;
                    case "FirstValue":
                    case "FirstValueAsync":
                        TranslateAndPushFirstValuePredicate(exp, aggregation.Predicates);
                        break;
                    case "RandomSample":
                    case "RandomSampleAsync":
                        TranslateAndPushTwoArgumentReductionPredicate(exp, ReduceFunction.RANDOM_SAMPLE, aggregation.Predicates);
                        break;
                    case "Load":
                        TranslateAndPushLoad(aggregation.Predicates, exp);
                        break;
                    case "LoadAll":
                        aggregation.Predicates.Push(new LoadAll());
                        break;
                }
            }

            return aggregation;
        }

        /// <summary>
        /// Build's a query from the given expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="type">The root type.</param>
        /// <param name="mainBooleanExpression">The primary boolean expression to build the filter from.</param>
        /// <param name="prefix">The index to use when creating the indexName.</param>
        /// <returns>A Redis query.</returns>
        /// <exception cref="InvalidOperationException">Thrown if type is missing indexing.</exception>
        internal static RedisQuery BuildQueryFromExpression(Expression expression, Type type, Expression? mainBooleanExpression, string prefix)
        {
            var attr = type.GetCustomAttribute<DocumentAttribute>();
            if (attr == null)
            {
                throw new InvalidOperationException("Searches can only be performed on objects decorated with a RedisObjectDefinitionAttribute that specifies a particular index");
            }

            var prefixAddendum = string.IsNullOrEmpty(prefix) ? string.Empty : $"-{prefix}";
            var indexName = string.IsNullOrEmpty(attr.IndexName) ? $"{type.Name.ToLower()}{prefixAddendum}-idx" : $"{attr.IndexName}{prefixAddendum}";
            var query = new RedisQuery(indexName!) { QueryText = "*" };
            switch (expression)
            {
                case MethodCallExpression methodExpression:
                {
                    var expressions = new List<MethodCallExpression> { methodExpression };
                    while (methodExpression.Arguments[0] is MethodCallExpression innerExpression)
                    {
                        expressions.Add(innerExpression);
                        methodExpression = innerExpression;
                    }

                    foreach (var exp in expressions)
                    {
                        switch (exp.Method.Name)
                        {
                            case "OrderBy":
                                query.SortBy = TranslateOrderByMethod(exp, true);
                                break;
                            case "OrderByDescending":
                                query.SortBy = TranslateOrderByMethod(exp, false);
                                break;
                            case "Select":
                                query.Return = TranslateSelectMethod(exp);
                                break;
                            case "Take":
                                query.Limit ??= new SearchLimit { Offset = 0 };
                                query.Limit.Number = TranslateTake(exp);
                                break;
                            case "Skip":
                                query.Limit ??= new SearchLimit { Number = 100 };
                                query.Limit.Offset = TranslateSkip(exp);
                                break;
                            case "First":
                            case "Any":
                            case "FirstOrDefault":
                                query.Limit ??= new SearchLimit { Offset = 0 };
                                query.Limit.Number = 1;
                                break;
                            case "GeoFilter":
                                query.GeoFilter = ExpressionParserUtilities.TranslateGeoFilter(exp);
                                break;
                        }
                    }

                    break;
                }

                case LambdaExpression lambda:
                    query.QueryText = BuildQueryFromExpression(lambda.Body);
                    break;
            }

            query.QueryText = mainBooleanExpression == null ? "*" : BuildQueryFromExpression(
                ((LambdaExpression)mainBooleanExpression).Body);

            return query;
        }

        /// <summary>
        /// Get's the index field type for the given member info.
        /// </summary>
        /// <param name="member">member to get the type for.</param>
        /// <returns>The index field type.</returns>
        internal static SearchFieldType DetermineIndexFieldsType(MemberInfo member)
        {
            if (member is PropertyInfo info)
            {
                if (TypeDeterminationUtilities.IsNumeric(info.PropertyType))
                {
                    return SearchFieldType.NUMERIC;
                }

                if (info.PropertyType.IsEnum)
                {
                    return TypeDeterminationUtilities.GetSearchFieldFromEnumProperty(info);
                }
            }

            return SearchFieldType.TAG;
        }

        /// <summary>
        /// Translates a binary expression.
        /// </summary>
        /// <param name="binExpression">The Binary Expression.</param>
        /// <returns>The query string formatted from the binary expression.</returns>
        /// <exception cref="ArgumentException">Thrown if expression is not parsable because of the arguments passed into it.</exception>
        internal static string TranslateBinaryExpression(BinaryExpression binExpression)
        {
            var sb = new StringBuilder();
            if (binExpression.Left is BinaryExpression leftBin && binExpression.Right is BinaryExpression rightBin)
            {
                sb.Append("(");
                sb.Append(TranslateBinaryExpression(leftBin));
                sb.Append(SplitPredicateSeporators(binExpression.NodeType));
                sb.Append(TranslateBinaryExpression(rightBin));
                sb.Append(")");
            }
            else if (binExpression.Left is BinaryExpression left)
            {
                sb.Append("(");
                sb.Append(TranslateBinaryExpression(left));
                sb.Append(SplitPredicateSeporators(binExpression.NodeType));
                sb.Append(ExpressionParserUtilities.GetOperandStringForQueryArgs(binExpression.Right));
                sb.Append(")");
            }
            else if (binExpression.Right is BinaryExpression right)
            {
                sb.Append("(");
                sb.Append(ExpressionParserUtilities.GetOperandStringForQueryArgs(binExpression.Left));
                sb.Append(SplitPredicateSeporators(binExpression.NodeType));
                sb.Append(TranslateBinaryExpression(right));
                sb.Append(")");
            }
            else
            {
                var leftContent = ExpressionParserUtilities.GetOperandStringForQueryArgs(binExpression.Left);

                var rightContent = ExpressionParserUtilities.GetOperandStringForQueryArgs(binExpression.Right);

                if (binExpression.Left is MemberExpression member)
                {
                    var predicate = BuildQueryPredicate(binExpression.NodeType, leftContent, rightContent, member);
                    sb.Append("(");
                    sb.Append(predicate);
                    sb.Append(")");
                }
                else if (binExpression.Left is UnaryExpression uni)
                {
                    member = (MemberExpression)uni.Operand;
                    var attr = member.Member.GetCustomAttributes(typeof(JsonConverterAttribute)).FirstOrDefault() as JsonConverterAttribute;
                    if (attr != null && attr.ConverterType == typeof(JsonStringEnumConverter))
                    {
                        if (int.TryParse(rightContent, out int ordinal))
                        {
                            rightContent = Enum.ToObject(member.Type, ordinal).ToString();
                        }
                    }
                    else
                    {
                        if (!int.TryParse(rightContent, out _) && !long.TryParse(rightContent, out _))
                        {
                            rightContent = ((int)Enum.Parse(member.Type, rightContent)).ToString();
                        }
                    }

                    var predicate = BuildQueryPredicate(binExpression.NodeType, leftContent, rightContent, member);
                    sb.Append("(");
                    sb.Append(predicate);
                    sb.Append(")");
                }
                else if (binExpression.Left is MethodCallExpression)
                {
                    sb.Append("(");
                    sb.Append(leftContent);
                    sb.Append(SplitPredicateSeporators(binExpression.NodeType));
                    sb.Append(rightContent);
                    sb.Append(")");
                }
                else
                {
                    throw new ArgumentException("Left side of expression must be a member of the search class");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get's the field name referred to by the expression.
        /// </summary>
        /// <param name="exp">The expression.</param>
        /// <returns>The field name.</returns>
        /// <exception cref="ArgumentException">Thrown if the expression is of an unexpected type.</exception>
        private static string GetFieldName(Expression exp)
        {
            if (exp is ConstantExpression constExp)
            {
                return constExp.Value.ToString();
            }

            if (exp is MemberExpression member)
            {
                return $"{member.Member.Name}";
            }

            if (exp is MethodCallExpression method)
            {
                return $"{((ConstantExpression)method.Arguments[0]).Value}";
            }

            if (exp is UnaryExpression unary)
            {
                return GetFieldName(unary.Operand);
            }

            if (exp is LambdaExpression lambda)
            {
                return GetFieldName(lambda.Body);
            }

            throw new ArgumentException("Invalid expression type detected when parsing Field Name");
        }

        /// <summary>
        /// Get's the field names for a group by expression.
        /// </summary>
        /// <param name="exp">The expression.</param>
        /// <returns>The field names.</returns>
        /// <exception cref="ArgumentException">Thrown if the expression is of an unrecognized type.</exception>
        private static string[] GetFieldNamesForExpression(Expression exp)
        {
            if (exp is ConstantExpression constExp)
            {
                return new[] { constExp.Value.ToString() };
            }

            if (exp is MemberExpression member)
            {
                return new[] { $"{member.Member.Name}" };
            }

            if (exp is MethodCallExpression method)
            {
                return new[] { $"{((ConstantExpression)method.Arguments[0]).Value}" };
            }

            if (exp is UnaryExpression unary)
            {
                return GetFieldNamesForExpression(unary.Operand);
            }

            if (exp is LambdaExpression lambda)
            {
                return GetFieldNamesForExpression(lambda.Body);
            }

            if (exp is NewExpression newExpression)
            {
                return newExpression.Members != null ? newExpression.Members.Select(x => $"{x.Name}").ToArray() : Array.Empty<string>();
            }

            throw new ArgumentException("Invalid expression type detected");
        }

        private static void TranslateAndPushLoad(Stack<IAggregationPredicate> predicates, MethodCallExpression expression)
        {
            var properties = GetFieldNamesForExpression(expression.Arguments[1]);
            if (properties.Length < 1)
            {
                throw new ArgumentException("Load predicate must contain at least 1 property");
            }

            predicates.Push(new Load(properties));
        }

        /// <summary>
        /// Translate and push a group by expression.
        /// </summary>
        /// <param name="predicates">Preexisting predicates for the aggregation.</param>
        /// <param name="expression">The expression to parse.</param>
        private static void TranslateAndPushGroupBy(Stack<IAggregationPredicate> predicates, MethodCallExpression expression)
        {
            var properties = GetFieldNamesForExpression(expression.Arguments[1]);
            if (predicates.Count > 0 && predicates.Peek() is GroupBy)
            {
                var gb = (GroupBy)predicates.Pop();
                var p = new List<string>();
                p.AddRange(properties);
                p.AddRange(gb.Properties);
                gb.Properties = p.ToArray();
                predicates.Push(gb);
            }
            else
            {
                predicates.Push(new GroupBy(properties));
            }
        }

        private static bool CheckForGroupby(Stack<IAggregationPredicate> predicates)
        {
            return predicates.Count == 0 || (predicates.Peek() is not GroupBy && predicates.Peek() is not SingleArgumentReduction);
        }

        private static bool CheckMoveGroupBy(Stack<IAggregationPredicate> predicates)
        {
            return predicates.Count > 0 && predicates.Peek() is GroupBy;
        }

        private static IAggregationPredicate TranslateApplyPredicate(MethodCallExpression exp)
        {
            var alias = ((ConstantExpression)exp.Arguments[2]).Value.ToString();
            var lambda = (LambdaExpression)((UnaryExpression)exp.Arguments[1]).Operand;
            return new Apply(lambda.Body, alias);
        }

        private static void PushAggregateSortBy(MethodCallExpression expression, SortDirection dir, Stack<IAggregationPredicate> operationStack)
        {
            var sb = TranslateSortBy(expression, dir);
            if (operationStack.Any() && operationStack.Peek() is MultiSort ms)
            {
                ms.InsertPredicate(sb);
            }
            else
            {
                ms = new MultiSort();
                ms.InsertPredicate(sb);
                operationStack.Push(ms);
            }
        }

        private static AggregateSortBy TranslateSortBy(MethodCallExpression expression, SortDirection dir)
        {
            var member = GetFieldName(expression.Arguments[1]);
            var sb = new AggregateSortBy(member, dir);
            return sb;
        }

        private static void PushReduction(Reduction reduction, Stack<IAggregationPredicate> operationStack)
        {
            var pushGroupBy = CheckForGroupby(operationStack);
            var moveGroupBy = CheckMoveGroupBy(operationStack);
            if (moveGroupBy)
            {
                var gb = operationStack.Pop();
                operationStack.Push(reduction);
                operationStack.Push(gb);
            }
            else
            {
                operationStack.Push(reduction);
                if (pushGroupBy)
                {
                    operationStack.Push(new GroupBy(Array.Empty<string>()));
                }
            }
        }

        private static void TranslateAndPushZeroArgumentPredicate(ReduceFunction function, Stack<IAggregationPredicate> stack)
        {
            var reduction = new ZeroArgumentReduction(function);
            PushReduction(reduction, stack);
        }

        private static void TranslateAndPushReductionPredicate(MethodCallExpression expression, ReduceFunction function, Stack<IAggregationPredicate> stack)
        {
            var member = GetFieldName(expression.Arguments[1]);
            var reduction = new SingleArgumentReduction(function, member);
            PushReduction(reduction, stack);
        }

        private static void TranslateAndPushFirstValuePredicate(MethodCallExpression expression, Stack<IAggregationPredicate> stack)
        {
            var reduction = new FirstValueReduction(expression);
            PushReduction(reduction, stack);
        }

        private static void TranslateAndPushTwoArgumentReductionPredicate(MethodCallExpression expression, ReduceFunction function, Stack<IAggregationPredicate> stack)
        {
            var reduction = new TwoArgumentReduction(function, expression);
            PushReduction(reduction, stack);
        }

        private static int TranslateTake(MethodCallExpression exp) => (int)((ConstantExpression)exp.Arguments[1]).Value;

        private static int TranslateSkip(MethodCallExpression exp) => (int)((ConstantExpression)exp.Arguments[1]).Value;

        private static ReturnFields TranslateSelectMethod(MethodCallExpression expression)
        {
            var predicate = (UnaryExpression)expression.Arguments[1];
            var lambda = (LambdaExpression)predicate.Operand;

            if (lambda.Body is MemberExpression member)
            {
                var properties = new[] { member.Member.Name };
                return new ReturnFields(properties);
            }
            else
            {
                var properties = lambda.ReturnType.GetProperties().Select(x => x.Name);
                return new ReturnFields(properties);
            }
        }

        private static RedisSortBy TranslateOrderByMethod(MethodCallExpression expression, bool ascending)
        {
            var sb = new RedisSortBy();
            var predicate = (UnaryExpression)expression.Arguments[1];
            var lambda = (LambdaExpression)predicate.Operand;
            var memberExpression = (MemberExpression)lambda.Body;
            sb.Field = memberExpression.Member.Name;
            sb.Direction = ascending ? SortDirection.Ascending : SortDirection.Descending;
            return sb;
        }

        private static string BuildQueryFromExpression(Expression exp)
        {
            if (exp is BinaryExpression binExp)
            {
                return TranslateBinaryExpression(binExp);
            }

            if (exp is MethodCallExpression method)
            {
                return ExpressionParserUtilities.TranslateMethodExpressions(method);
            }

            if (exp is UnaryExpression uni)
            {
                var operandString = BuildQueryFromExpression(uni.Operand);
                if (uni.NodeType == ExpressionType.Not)
                {
                    operandString = $"-{operandString}";
                }

                return operandString;
            }

            if (exp is MemberExpression member && member.Type == typeof(bool))
            {
                var property = ExpressionParserUtilities.GetOperandString(exp);
                return $"{property}:{{true}}";
            }

            throw new ArgumentException("Unparseable Lambda Body detected");
        }

        private static string TranslateFirstMethod(MethodCallExpression expression)
        {
            var predicate = (UnaryExpression)expression.Arguments[1];
            var lambda = (LambdaExpression)predicate.Operand;
            return BuildQueryFromExpression(lambda.Body);
        }

        private static string TranslateWhereMethod(MethodCallExpression expression)
        {
            var predicate = (UnaryExpression)expression.Arguments[1];
            var lambda = (LambdaExpression)predicate.Operand;
            return BuildQueryFromExpression(lambda.Body);
        }

        private static string BuildQueryPredicate(ExpressionType expType, string left, string right, MemberExpression memberExpression)
        {
            var queryPredicate = expType switch
            {
                ExpressionType.GreaterThan => $"{left}:[({right} inf]",
                ExpressionType.LessThan => $"{left}:[-inf ({right}]",
                ExpressionType.GreaterThanOrEqual => $"{left}:[{right} inf]",
                ExpressionType.LessThanOrEqual => $"{left}:[-inf {right}]",
                ExpressionType.Equal => BuildEqualityPredicate(memberExpression, right),
                ExpressionType.NotEqual => BuildEqualityPredicate(memberExpression, right, true),
                _ => string.Empty
            };
            return queryPredicate;
        }

        private static string BuildEqualityPredicate(MemberExpression member, string right, bool negated = false)
        {
            var sb = new StringBuilder();
            var fieldAttribute = ExpressionParserUtilities.DetermineSearchAttribute(member);
            if (fieldAttribute == null)
            {
                throw new InvalidOperationException("Searches can only be performed on fields marked with a " +
                                                    "RedisFieldAttribute with the SearchFieldType not set to None");
            }

            if (negated)
            {
                sb.Append("-");
            }

            sb.Append($"@{ExpressionParserUtilities.GetSearchFieldNameFromMember(member)}:");
            var searchFieldType = fieldAttribute.SearchFieldType != SearchFieldType.INDEXED
                ? fieldAttribute.SearchFieldType
                : DetermineIndexFieldsType(member.Member);
            switch (searchFieldType)
            {
                case SearchFieldType.TAG:
                    sb.Append($"{{{ExpressionParserUtilities.EscapeTagField(right)}}}");
                    break;
                case SearchFieldType.TEXT:
                    sb.Append($"\"{right}\"");
                    break;
                case SearchFieldType.NUMERIC:
                    sb.Append($"[{right} {right}]");
                    break;
                default:
                    throw new InvalidOperationException("Could not translate query, equality searches only supported for Tag and numeric fields");
            }

            return sb.ToString();
        }

        private static string SplitPredicateSeporators(ExpressionType type) => type switch
        {
            ExpressionType.OrElse => " | ",
            ExpressionType.AndAlso => " ",
            _ => throw new ArgumentException("Unknown separator type")
        };
    }
}
