using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM.Modeling;
using Redis.OM.Searching.Query;

namespace Redis.OM.Common
{
    /// <summary>
    /// utilities for parsing expressions.
    /// </summary>
    internal static class ExpressionParserUtilities
    {
        /// <summary>
        /// Characters to escape when serializing a tag expression.
        /// </summary>
        private static readonly char[] TagEscapeChars =
        {
            ',', '.', '<', '>', '{', '}', '[', ']', '"', '\'', ':', ';',
            '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '~', '|', ' ', '/',
        };

        /// <summary>
        /// Get's the operand string.
        /// </summary>
        /// <param name="exp">the expression to parse.</param>
        /// <returns>The operand string.</returns>
        internal static string GetOperandString(Expression exp)
        {
            return exp switch
            {
                ConstantExpression constExp => ValueToString(constExp.Value),
                MemberExpression member => GetOperandStringForMember(member),
                MethodCallExpression method when method.Method.Name == "get_Item" =>
                    $"@{((ConstantExpression)method.Arguments[0]).Value}",
                MethodCallExpression method => GetOperandString(method),
                UnaryExpression unary => GetOperandString(unary.Operand),
                BinaryExpression binExpression => ParseBinaryExpression(binExpression),
                LambdaExpression lambda => GetOperandString(lambda.Body),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the operand string.
        /// </summary>
        /// <param name="exp">the expression to parse.</param>
        /// <returns>The operand string.</returns>
        internal static string GetOperandString(MethodCallExpression exp)
        {
            var mathMethods = new List<string> { "log", "abs", "ceil", "floor", "log2", "exp", "sqrt" };
            var methodName = MapMethodName(exp.Method.Name);

            return methodName switch
            {
                "split" => ParseSplitMethod(exp),
                "format" => ParseFormatMethod(exp),
                _ => mathMethods.Contains(methodName) ? ParseMathMethod(exp, methodName) : ParseMethod(exp, methodName)
            };
        }

        /// <summary>
        /// Gets the operand string from a search.
        /// </summary>
        /// <param name="exp">expression.</param>
        /// <param name="treatEnumsAsInt">Treat enum as an integer.</param>
        /// <param name="negate">Whether or not to negate the result.</param>
        /// <returns>the operand string.</returns>
        /// <exception cref="ArgumentException">thrown if expression is un-parseable.</exception>
        internal static string GetOperandStringForQueryArgs(Expression exp, bool treatEnumsAsInt = false, bool negate = false)
        {
            var res = exp switch
            {
                ConstantExpression constExp => $"{constExp.Value}",
                MemberExpression member => GetOperandStringForMember(member, treatEnumsAsInt),
                MethodCallExpression method => TranslateMethodStandardQuerySyntax(method),
                UnaryExpression unary => GetOperandStringForQueryArgs(unary.Operand, treatEnumsAsInt, unary.NodeType == ExpressionType.Not),
                _ => throw new ArgumentException("Unrecognized Expression type")
            };

            if (negate)
            {
                return $"-{res}";
            }

            return res;
        }

        /// <summary>
        /// Pull the value out of a member, typically used for a closure.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="forObject">the object.</param>
        /// <returns>the value.</returns>
        /// <exception cref="NotImplementedException">thrown if member info is not a field or property.</exception>
        internal static object GetValue(MemberInfo memberInfo, object forObject)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(forObject);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Determines whether it's a binary expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>Whether or not it's a binary expression.</returns>
        internal static bool IsBinaryExpression(Expression expression)
        {
            if (expression is BinaryExpression)
            {
                return true;
            }

            if (expression is UnaryExpression uni && uni.Operand is BinaryExpression)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Splits the expression apart into a query.
        /// </summary>
        /// <param name="rootBinaryExpression">The root expression.</param>
        /// <param name="filterFormat">Whether or not to use the filter format.</param>
        /// <returns>a query.</returns>
        internal static string ParseBinaryExpression(BinaryExpression rootBinaryExpression, bool filterFormat = false)
        {
            var operationStack = new Stack<string>();
            var binExpressions = SplitBinaryExpression(rootBinaryExpression);
            foreach (var expression in binExpressions)
            {
                var right = GetOperandString(expression.Right);
                var left = GetOperandString(expression.Left);
                if (filterFormat && ((expression.Left is MemberExpression mem &&
                                      mem.Type == typeof(string)) || (expression.Left is UnaryExpression uni &&
                                                                      uni.Type == typeof(string))))
                {
                    right = $"'{right}'";
                }

                operationStack.Push(right);
                operationStack.Push(GetOperatorFromNodeType(expression.NodeType));
                if (!string.IsNullOrEmpty(left) && !IsBinaryExpression(expression.Left))
                {
                    operationStack.Push(left);
                }
            }

            return string.Join(" ", operationStack);
        }

        /// <summary>
        /// Translates the method expression.
        /// </summary>
        /// <param name="exp">the expression.</param>
        /// <returns>The expression translated.</returns>
        /// <exception cref="ArgumentException">thrown if the method isn't recognized.</exception>
        internal static string TranslateMethodExpressions(MethodCallExpression exp)
        {
            return exp.Method.Name switch
            {
                "Contains" => TranslateContainsStandardQuerySyntax(exp),
                nameof(StringExtension.FuzzyMatch) => TranslateFuzzyMatch(exp),
                nameof(StringExtension.MatchContains) => TranslateMatchContains(exp),
                nameof(StringExtension.MatchStartsWith) => TranslateMatchStartsWith(exp),
                nameof(StringExtension.MatchEndsWith) => TranslateMatchEndsWith(exp),
                nameof(string.StartsWith) => TranslateStartsWith(exp),
                nameof(string.EndsWith) => TranslateEndsWith(exp),
                "Any" => TranslateAnyForEmbeddedObjects(exp),
                _ => throw new ArgumentException($"Unrecognized method for query translation:{exp.Method.Name}")
            };
        }

        /// <summary>
        /// Translates a method expression into a geofilter.
        /// </summary>
        /// <param name="exp">the expression.</param>
        /// <returns>the geo filter.</returns>
        internal static RedisGeoFilter TranslateGeoFilter(MethodCallExpression exp)
        {
            var memberOperand = GetOperandString(exp.Arguments[1]).Substring(1);
            var longitude = (double)((ConstantExpression)exp.Arguments[2]).Value;
            var latitude = (double)((ConstantExpression)exp.Arguments[3]).Value;
            var radius = (double)((ConstantExpression)exp.Arguments[4]).Value;
            var unit = (GeoLocDistanceUnit)((ConstantExpression)exp.Arguments[5]).Value;
            return new RedisGeoFilter(memberOperand, longitude, latitude, radius, unit);
        }

        /// <summary>
        /// Gets the search field name from member expression. Will climb back up to the parent node and build the alias.
        /// Which will be all the names in the path to the expression seperated by an underscore. e.g. Address_City.
        /// </summary>
        /// <param name="member">The member expression to pull the serach field name from.</param>
        /// <returns>The alias to search for.</returns>
        internal static string GetSearchFieldNameFromMember(MemberExpression member)
        {
            var stack = GetMemberChain(member);
            var topMember = stack.Peek();
            var memberPath = stack.Select(x => x.Name).ToArray();

            if (topMember == member.Member)
            {
                var searchField = member.Member.GetCustomAttributes().Where(x => x is SearchFieldAttribute).Cast<SearchFieldAttribute>().FirstOrDefault();
                if (searchField != null && !string.IsNullOrEmpty(searchField.PropertyName))
                {
                    return searchField.PropertyName;
                }
            }

            return string.Join("_", memberPath);
        }

        /// <summary>
        /// Gets the chain of members down to the currently accessed member.
        /// </summary>
        /// <param name="memberExpression">The member expression being accessed.</param>
        /// <returns>The chain of members down to the currently accessed member, e.g. if a Person's
        /// Address.City was being accessed a stack with Address at the top and City at the bottom would be returned.</returns>
        internal static Stack<MemberInfo> GetMemberChain(MemberExpression memberExpression)
        {
            var memberStack = new Stack<MemberInfo>();
            memberStack.Push(memberExpression.Member);

            var parentExpression = memberExpression.Expression;
            while (parentExpression is MemberExpression parentMember)
            {
                if (parentMember.Member.Name == nameof(AggregationResult<object>.RecordShell))
                {
                    break;
                }

                memberStack.Push(parentMember.Member);
                parentExpression = parentMember.Expression;
            }

            return memberStack;
        }

        /// <summary>
        /// Gets the Search Field type for the member.
        /// </summary>
        /// <param name="memberExpression">the member expression.</param>
        /// <returns>the <see cref="SearchFieldAttribute"/>.</returns>
        internal static SearchFieldAttribute? DetermineSearchAttribute(MemberExpression memberExpression)
        {
            var memberChain = GetMemberChain(memberExpression);
            SearchFieldAttribute? attr;
            do
            {
                var memberInfo = memberChain.Pop();
                attr = memberInfo
                    .GetCustomAttributes()
                    .Where(x => x is SearchFieldAttribute)
                    .Cast<SearchFieldAttribute>()
                    .FirstOrDefault(x => x.JsonPath?.Split('.').Last() == memberExpression.Member.Name);
            }
            while (attr == null && memberChain.Any());

            if (attr == null)
            {
                attr = memberExpression.Member.GetCustomAttributes().Where(x => x is SearchFieldAttribute).Cast<SearchFieldAttribute>().FirstOrDefault();
            }

            return attr;
        }

        /// <summary>
        /// Escapes a tag field string.
        /// </summary>
        /// <param name="text">the text toe escape.</param>
        /// <returns>The Escaped Text.</returns>
        internal static string EscapeTagField(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (TagEscapeChars.Contains(c))
                {
                    sb.Append("\\");
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string GetOperandStringForMember(MemberExpression member, bool treatEnumsAsInt = false)
        {
            var memberPath = new List<string>();
            var parentExpression = member.Expression;
            while (parentExpression is MemberExpression parentMember)
            {
                memberPath.Add(parentMember.Member.Name);
                parentExpression = parentMember.Expression;
            }

            memberPath.Add(member.Member.Name);

            var searchField = member.Member.GetCustomAttributes().Where(x => x is SearchFieldAttribute).Cast<SearchFieldAttribute>().FirstOrDefault();

            var dependencyChain = new List<MemberExpression>();
            var pointingExpression = member;
            while (pointingExpression != null)
            {
                dependencyChain.Add(pointingExpression);
                pointingExpression = pointingExpression.Expression as MemberExpression;
            }

            if (dependencyChain.Last().Expression is ConstantExpression c)
            {
                var resolved = c.Value;

                for (var i = dependencyChain.Count; i > 0; i--)
                {
                    var expr = dependencyChain[i - 1];
                    resolved = GetValue(expr.Member, resolved);
                }

                var resolvedType = resolved.GetType();

                if (resolved is IEnumerable<string> strings)
                {
                    return string.Join("|", strings);
                }

                if (resolved is IEnumerable<Guid> guids)
                {
                    return string.Join("|", guids);
                }

                if (resolved is IEnumerable<Ulid> ulids)
                {
                    return string.Join("|", ulids);
                }

                if (resolvedType.IsArray || resolvedType.GetInterfaces().Contains(typeof(IEnumerable)))
                {
                    var asEnumerable = (IEnumerable)resolved;
                    var elementType = resolvedType.GetElementType();
                    if (elementType == null)
                    {
                        elementType = resolvedType.GenericTypeArguments.FirstOrDefault();
                    }

                    if (elementType != null && TypeDeterminationUtilities.IsNumeric(elementType))
                    {
                        var sb = new StringBuilder();
                        sb.Append('|');

                        foreach (var item in asEnumerable)
                        {
                            sb.Append(FormattableString.Invariant($"[{item} {item}]|"));
                        }

                        sb.Remove(sb.Length - 1, 1);
                        return sb.ToString();
                    }

                    if (elementType != null && elementType.IsEnum)
                    {
                        if (treatEnumsAsInt)
                        {
                            var sb = new StringBuilder();
                            sb.Append('|');
                            foreach (var item in asEnumerable)
                            {
                                var asInt = (int)item;
                                sb.Append($"[{asInt} {asInt}]|");
                            }

                            sb.Remove(sb.Length - 1, 1);
                            return sb.ToString();
                        }
                        else
                        {
                            var strs = new List<string>();
                            foreach (var item in asEnumerable)
                            {
                                strs.Add(item.ToString());
                            }

                            return string.Join("|", strs);
                        }
                    }
                }

                return ValueToString(resolved);
            }

            if (searchField != null)
            {
                var propertyName = GetSearchFieldNameFromMember(member);
                return $"@{propertyName}";
            }

            throw new InvalidOperationException(
                $"Could not retrieve value from {member.Member.Name}, most likely, it is not properly decorated in the model defining the index.");
        }

        private static string GetOperandStringStringArgs(Expression exp)
        {
            return exp switch
            {
                ConstantExpression constExp => GetConstantStringForArgs(constExp),
                MemberExpression member => GetOperandStringForMember(member),
                MethodCallExpression method => $"@{((ConstantExpression)method.Arguments[0]).Value}",
                UnaryExpression unary => GetOperandString(unary.Operand),
                BinaryExpression binExpression => ParseBinaryExpression(binExpression),
                _ => string.Empty
            };
        }

        private static string MapMethodName(string methodName) => methodName switch
        {
            nameof(string.ToUpper) => "upper",
            nameof(string.ToLower) => "lower",
            nameof(string.Contains) => "contains",
            nameof(string.StartsWith) => "startswith",
            nameof(string.Substring) => "substr",
            nameof(string.Format) => "format",
            nameof(string.Split) => "split",
            nameof(Math.Log) => "log2",
            nameof(Math.Log10) => "log",
            nameof(Math.Ceiling) => "ceil",
            nameof(Math.Floor) => "floor",
            nameof(Math.Exp) => "exp",
            nameof(Math.Abs) => "abs",
            nameof(Math.Sqrt) => "sqrt",
            nameof(ApplyFunctions.Day) => "day",
            nameof(ApplyFunctions.Hour) => "hour",
            nameof(ApplyFunctions.Minute) => "minute",
            nameof(ApplyFunctions.Month) => "month",
            nameof(ApplyFunctions.DayOfWeek) => "dayofweek",
            nameof(ApplyFunctions.DayOfMonth) => "dayofmonth",
            nameof(ApplyFunctions.DayOfYear) => "dayofyear",
            nameof(ApplyFunctions.Year) => "year",
            nameof(ApplyFunctions.MonthOfYear) => "monthofyear",
            nameof(ApplyFunctions.FormatTimestamp) => "timefmt",
            nameof(ApplyFunctions.ParseTime) => "parsetime",
            nameof(ApplyFunctions.GeoDistance) => "geodistance",
            nameof(ApplyFunctions.Exists) => "exists",
            _ => string.Empty
        };

        private static string ParseMathMethod(MethodCallExpression exp, string methodName)
        {
            var sb = new StringBuilder();
            sb.Append($"{methodName}(");
            sb.Append(GetOperandString(exp.Arguments[0]));
            sb.Append(")");
            return sb.ToString();
        }

        private static string ParseMethod(MethodCallExpression exp, string methodName)
        {
            var sb = new StringBuilder();
            var args = new List<string>();
            sb.Append($"{methodName}(");
            if (exp.Object != null)
            {
                args.Add(GetOperandStringStringArgs(exp.Object));
            }

            args.AddRange(exp.Arguments.Select(GetOperandStringStringArgs));
            sb.Append(string.Join(",", args));
            if (methodName == "substr" && args.Count == 2)
            {
                sb.Append(",-1");
            }

            sb.Append(")");
            return sb.ToString();
        }

        private static string ParseFormatMethod(MethodCallExpression exp)
        {
            var pattern = "\\{(\\d+|)\\}";
            var args = new List<string>();
            var sb = new StringBuilder();
            sb.Append("format(");
            var formatStringExpression = exp.Arguments[0];
            var formatArgs = new List<string>();
            string formatString = string.Empty;
            switch (formatStringExpression)
            {
                case ConstantExpression constantFormattedExpression:
                    formatString = constantFormattedExpression.Value.ToString();
                    args.Add($"\"{Regex.Replace(formatString, pattern, "%s")}\"");
                    break;

                case MemberExpression { Expression: ConstantExpression constInnerExpression } member:
                    formatString = (string)GetValue(member.Member, constInnerExpression.Value);
                    args.Add($"\"{Regex.Replace(formatString, pattern, "%s")}\"");
                    break;
            }

            for (var i = 1; i < exp.Arguments.Count; i++)
            {
                formatArgs.Add(GetOperandStringStringArgs(exp.Arguments[i]));
            }

            var matches = Regex.Matches(formatString, pattern);
            args.AddRange(from Match? match in matches
                select match.Value.Substring(1, match.Length - 2)
                into subStr
                select int.Parse(subStr)
                into matchIndex
                select formatArgs[matchIndex]);
            sb.Append(string.Join(",", args));
            sb.Append(")");
            return sb.ToString();
        }

        private static string GetOperatorFromNodeType(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                ExpressionType.Power => "^",
                ExpressionType.ExclusiveOr => "^",
                ExpressionType.Multiply => "*",
                ExpressionType.Equal => "==",
                ExpressionType.OrElse => "||",
                ExpressionType.AndAlso => "&&",
                ExpressionType.Not => "!",
                ExpressionType.NotEqual => "!=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                _ => string.Empty
            };
        }

        private static string ParseSplitMethod(MethodCallExpression exp)
        {
            var args = new List<string>();
            var sb = new StringBuilder();
            sb.Append("split(");
            args.Add(GetOperandStringStringArgs(exp.Object ??
                                                throw new InvalidOperationException(
                                                    "Object within expression is null.")));
            var arg = exp.Arguments[0];
            if (arg.Type == typeof(string) || arg.Type == typeof(char))
            {
                args.Add($"\"{GetOperandStringStringArgs(arg)}\"");
            }
            else
            {
                switch (arg)
                {
                    case MemberExpression { Expression: ConstantExpression constExp } member:
                    {
                        var innerArgList = new List<string>();
                        if (member.Type == typeof(char[]))
                        {
                            var charArr = (char[])GetValue(member.Member, constExp.Value);
                            innerArgList.AddRange(charArr.Select(c => c.ToString()));
                        }
                        else if (member.Type == typeof(string[]))
                        {
                            var stringArr = (string[])GetValue(member.Member, constExp.Value);
                            innerArgList.AddRange(stringArr);
                        }

                        args.Add($"\"{string.Join(",", innerArgList)}\"");
                        break;
                    }

                    case NewArrayExpression arrayExpression:
                    {
                        var innerArgList = new List<string>();
                        foreach (var item in arrayExpression.Expressions)
                        {
                            if (item is ConstantExpression constant)
                            {
                                innerArgList.Add(GetConstantStringForArgs(constant));
                            }
                        }

                        args.Add($"\"{string.Join(",", innerArgList)}\"");
                        break;
                    }
                }
            }

            sb.Append(string.Join(",", args));
            sb.Append(")");
            return sb.ToString();
        }

        private static IEnumerable<BinaryExpression> SplitBinaryExpression(BinaryExpression exp)
        {
            var list = new List<BinaryExpression>();
            do
            {
                list.Add(exp);
                switch (exp.Left)
                {
                    case UnaryExpression unExp:
                        if (unExp.Operand is BinaryExpression inner)
                        {
                            exp = inner;
                        }
                        else
                        {
                            return list;
                        }

                        break;
                    case BinaryExpression left:
                        exp = left;
                        break;
                    default:
                        return list;
                }
            }
            while (true);
        }

        private static string TranslateMethodStandardQuerySyntax(MethodCallExpression exp)
        {
            return exp.Method.Name switch
            {
                nameof(StringExtension.FuzzyMatch) => TranslateFuzzyMatch(exp),
                nameof(string.Format) => TranslateFormatMethodStandardQuerySyntax(exp),
                nameof(string.Contains) => TranslateContainsStandardQuerySyntax(exp),
                nameof(string.StartsWith) => TranslateStartsWith(exp),
                nameof(string.EndsWith) => TranslateEndsWith(exp),
                "Any" => TranslateAnyForEmbeddedObjects(exp),
                _ => throw new InvalidOperationException($"Unable to parse method {exp.Method.Name}")
            };
        }

        private static string TranslateFormatMethodStandardQuerySyntax(MethodCallExpression exp)
        {
            var format = GetOperandString(exp.Arguments[0]);
            string[] args;
            if (exp.Arguments[1] is NewArrayExpression newArrayExpression)
            {
                args = newArrayExpression.Expressions.Select(GetOperandString).ToArray();
            }
            else
            {
                args = new string[exp.Arguments.Count - 1];
                for (var i = 1; i < exp.Arguments.Count; i++)
                {
                    args[i - 1] = GetOperandString(exp.Arguments[i]);
                }
            }

            return string.Format(format, args);
        }

        private static bool IsFullTextSearch(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                return DetermineSearchAttribute(member) is SearchableAttribute;
            }

            return false;
        }

        private static string TranslateStartsWith(MethodCallExpression exp)
        {
            string source;
            string prefix;
            Expression sourceExpression;
            if (exp.Arguments.Count < 2 && exp.Object is not null)
            {
                source = GetOperandString(exp.Object);
                prefix = GetOperandString(exp.Arguments[0]);
                sourceExpression = exp.Object;
            }
            else if (exp.Arguments.Count >= 2)
            {
                source = GetOperandString(exp.Arguments[0]);
                prefix = GetOperandString(exp.Arguments[1]);
                sourceExpression = exp.Arguments[0];
            }
            else
            {
                throw new InvalidOperationException("Could not parse out StartsWith method from provided expression");
            }

            if (IsFullTextSearch(sourceExpression))
            {
                return $"({source}:{prefix}*)";
            }

            return $"({source}:{{{EscapeTagField(prefix)}*}})";
        }

        private static string TranslateEndsWith(MethodCallExpression exp)
        {
            string source;
            string suffix;
            Expression sourceExpression;
            if (exp.Arguments.Count < 2 && exp.Object is not null)
            {
                source = GetOperandString(exp.Object);
                suffix = GetOperandString(exp.Arguments[0]);
                sourceExpression = exp.Object;
            }
            else if (exp.Arguments.Count >= 2)
            {
                source = GetOperandString(exp.Arguments[0]);
                suffix = GetOperandString(exp.Arguments[1]);
                sourceExpression = exp.Arguments[0];
            }
            else
            {
                throw new InvalidOperationException("Could not parse out EndsWith method from provided expression");
            }

            if (IsFullTextSearch(sourceExpression))
            {
                return $"({source}:*{suffix})";
            }

            return $"({source}:{{*{EscapeTagField(suffix)}}})";
        }

        private static string TranslateMatchStartsWith(MethodCallExpression exp)
        {
            var source = GetOperandString(exp.Arguments[0]);
            var prefix = GetOperandString(exp.Arguments[1]);
            return $"({source}:{prefix}*)";
        }

        private static string TranslateMatchEndsWith(MethodCallExpression exp)
        {
            var source = GetOperandString(exp.Arguments[0]);
            var suffix = GetOperandString(exp.Arguments[1]);
            return $"({source}:*{suffix})";
        }

        private static string TranslateMatchContains(MethodCallExpression exp)
        {
            var source = GetOperandString(exp.Arguments[0]);
            var infix = GetOperandString(exp.Arguments[1]);
            return $"({source}:*{infix}*)";
        }

        private static string TranslateFuzzyMatch(MethodCallExpression exp)
        {
            var source = GetOperandString(exp.Arguments[0]);
            var term = GetOperandString(exp.Arguments[1]);
            if (!int.TryParse(GetOperandString(exp.Arguments[2]), out var distanceThreshold))
            {
                throw new ArgumentException($"Could not parse {nameof(distanceThreshold)}");
            }

            return distanceThreshold switch
            {
                1 => $"({source}:%{term}%)",
                2 => $"({source}:%%{term}%%)",
                3 => $"({source}:%%%{term}%%%)",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(distanceThreshold),
                    distanceThreshold,
                    $"{nameof(distanceThreshold)} must not exceed 3")
            };
        }

        private static string TranslateContainsStandardQuerySyntax(MethodCallExpression exp)
        {
            MemberExpression? expression = null;
            Type type;
            string memberName;
            string literal;
            SearchFieldAttribute? searchFieldAttribute = null;
            if (exp.Arguments.LastOrDefault() is MemberExpression && exp.Arguments.FirstOrDefault() is MemberExpression)
            {
                var propertyExpression = (MemberExpression)exp.Arguments.Last();
                var valuesExpression = (MemberExpression)exp.Arguments.First();
                literal = GetOperandStringForQueryArgs(propertyExpression);
                if (!literal.StartsWith("@"))
                {
                    if (exp.Arguments.Count == 1 && exp.Object != null)
                    {
                        propertyExpression = (MemberExpression)exp.Object;
                        valuesExpression = (MemberExpression)exp.Arguments.Single();
                    }
                    else
                    {
                        propertyExpression = (MemberExpression)exp.Arguments.First();
                        valuesExpression = (MemberExpression)exp.Arguments.Last();
                    }
                }
                else if (propertyExpression == valuesExpression)
                {
                    propertyExpression = (MemberExpression)exp.Arguments.First();
                    valuesExpression = (MemberExpression)exp.Object;
                }

                var attribute = DetermineSearchAttribute(propertyExpression);
                if (attribute == null)
                {
                    attribute = DetermineSearchAttribute(valuesExpression);
                    if (attribute != null)
                    {
                        propertyExpression = (MemberExpression)exp.Arguments.First();
                        valuesExpression = (MemberExpression)exp.Arguments.Last();
                    }
                    else
                    {
                        throw new InvalidOperationException("Called contains for a non-indexed property");
                    }
                }

                type = Nullable.GetUnderlyingType(propertyExpression.Type) ?? propertyExpression.Type;
                var valueType = Nullable.GetUnderlyingType(valuesExpression.Type) ?? valuesExpression.Type;
                memberName = GetOperandStringForMember(propertyExpression);
                var treatEnumsAsInts = type.IsEnum && !(propertyExpression.Member.GetCustomAttributes(typeof(JsonConverterAttribute)).FirstOrDefault() is JsonConverterAttribute converter && converter.ConverterType == typeof(JsonStringEnumConverter));
                literal = GetOperandStringForQueryArgs(valuesExpression, treatEnumsAsInts);

                if ((valueType == typeof(List<string>) || valueType == typeof(string[]) || type == typeof(string[]) || type == typeof(List<string>) || type == typeof(Guid) || type == typeof(Guid[]) || type == typeof(List<Guid>) || type == typeof(Guid[]) || type == typeof(List<Guid>) || type == typeof(Ulid) || (type.IsEnum && !treatEnumsAsInts)) && attribute is IndexedAttribute)
                {
                    return $"({memberName}:{{{EscapeTagField(literal).Replace("\\|", "|")}}})";
                }

                if (type == typeof(string) && attribute is IndexedAttribute)
                {
                    return $"({memberName}:{{*{EscapeTagField(literal)}*}})";
                }

                if (type == typeof(string) && attribute is SearchableAttribute)
                {
                    return $"({memberName}:{literal})";
                }

                var ret = literal.Replace("|", $"{memberName}:");
                ret = ret.Replace("]", "]|");
                ret = ret.Substring(0, ret.Length - 1);

                return ret;
            }

            if (exp.Object is MemberExpression)
            {
                expression = exp.Object as MemberExpression;
                if (expression is not null)
                {
                    searchFieldAttribute = DetermineSearchAttribute(expression);
                }
            }
            else if (exp.Arguments.FirstOrDefault() is MemberExpression)
            {
                expression = (MemberExpression)exp.Arguments.First();
                searchFieldAttribute = DetermineSearchAttribute(expression);
            }

            if (exp.Arguments.LastOrDefault() is MemberExpression memEx && exp.Arguments.FirstOrDefault() is ConstantExpression cs)
            {
                var propertyName = $"{GetOperandString(memEx)}";
                return $"({GetContainsStringForConstantExpression(propertyName, cs)})";
            }

            if (expression == null)
            {
                throw new InvalidOperationException($"Could not parse query for Contains");
            }

            type = Nullable.GetUnderlyingType(expression.Type) ?? expression.Type;
            memberName = GetOperandStringForMember(expression);
            literal = GetOperandStringForQueryArgs(exp.Arguments.Last());

            if (searchFieldAttribute is not null && searchFieldAttribute is SearchableAttribute)
            {
                return $"({memberName}:{literal})";
            }

            return (type == typeof(string)) ? $"({memberName}:{{*{EscapeTagField(literal)}*}})" : $"({memberName}:{{{EscapeTagField(literal)}}})";
        }

        private static string GetContainsStringForConstantExpression(string propertyNameOperand, ConstantExpression cs)
        {
            var enumerable = cs.Value as IEnumerable;
            if (enumerable is null)
            {
                throw new ArgumentException("Could not create contains predicate from non-enumerable value");
            }

            var isNumeric = TypeDeterminationUtilities.IsNumericEnumerable(enumerable);
            var sb = new StringBuilder();

            if (!isNumeric)
            {
                sb.Append($"{propertyNameOperand}:{{");
            }

            foreach (var o in enumerable)
            {
                if (isNumeric)
                {
                    sb.Append($"{propertyNameOperand}:[{o} {o}]");
                }
                else
                {
                    sb.Append(EscapeTagField(o.ToString()));
                }

                sb.Append("|");
            }

            sb.Remove(sb.Length - 1, 1);
            if (!isNumeric)
            {
                sb.Append("}");
            }

            return sb.ToString();
        }

        private static string TranslateAnyForEmbeddedObjects(MethodCallExpression exp)
        {
            var type = exp.Arguments.Last().Type;
            var prefix = GetOperandString(exp.Arguments[0]);
            var lambda = (LambdaExpression)exp.Arguments.Last();
            var tempQuery = ExpressionTranslator.TranslateBinaryExpression((BinaryExpression)lambda.Body);
            return tempQuery.Replace("@", $"{prefix}_");
        }

        private static string ValueToString(object value)
        {
            Type valueType = value.GetType();

            if (valueType == typeof(double) || Nullable.GetUnderlyingType(valueType) == typeof(double))
            {
                return ((double)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is DateTimeOffset dto)
            {
                return dto.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            }

            if (value is DateTime dt)
            {
                return new DateTimeOffset(dt).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private static string GetConstantStringForArgs(ConstantExpression constExp)
        {
            string valueAsString = ValueToString(constExp.Value);

            if (constExp.Type == typeof(string))
            {
                return $"\"{valueAsString}\"";
            }

            return $"{valueAsString}";
        }
    }
}