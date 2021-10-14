using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Reflection;

namespace NRedisPlus.RediSearch
{
    public static class ExpressionParserUtilities
    {
        internal static string GetOperandString(Expression exp)
        {
            if (exp is ConstantExpression constExp)
                return constExp.Value.ToString();
            if (exp is MemberExpression member)
                return GetOperandStringForMember(member);
            if (exp is MethodCallExpression method)
            {
                if (method.Method.Name == "get_Item")
                    return $"@{((ConstantExpression)method.Arguments[0]).Value}";
                else
                    return GetOperandString(method);
            }
            if (exp is UnaryExpression unary)
                return GetOperandString(unary.Operand);
            if (exp is BinaryExpression binExpression)
                return ParseBinaryExpression(binExpression);
            if (exp is LambdaExpression lambda)
                return GetOperandString(lambda.Body);
            return string.Empty;
        }

        internal static string GetOperandString(MethodCallExpression exp)
        {
            var stringMethods = new List<string> { "upper", "lower", "contains", "startswith", "substr", "format", "split" };
            var mathMethods = new List<string> { "log", "abs", "ceil", "floor", "log2", "exp", "sqrt" };
            var methodName = MapMethodName(exp.Method.Name);

            if (methodName == "split")
                return ParseSplitMethod(exp);
            if (methodName == "format")
                return ParseFormatMethod(exp);
            if (mathMethods.Contains(methodName))
                return ParseMathMethod(exp, methodName);
            return ParseMethod(exp, methodName);
        }
        internal static string GetOperandStringForMember(MemberExpression member, bool isSearch = false)
        {
            var searchField = member.Member.GetCustomAttribute<SearchFieldAttribute>();
            if (searchField == null)
            {
                if (member.Expression is ConstantExpression c)
                {
                    var val = GetValue(member.Member, c.Value);
                    if (val != null)
                    {
                        return val.ToString();
                    }

                }
                throw new ArgumentException("Operand for expression must either be an indexed " +
                    "field of a model, a literal, or must have a value when expression is enumerated");

            }
            else
            {
                // if (!searchField.Aggregatable && !searchField.Sortable && ! isSearch)
                //     throw new ArgumentException(
                //         "Indexed field must be explicitly marked as aggregateable in order to perform aggregaitons on it");
                var propertyName = string.IsNullOrEmpty(searchField.PropertyName) ? member.Member.Name : searchField.PropertyName;
                return $"@{propertyName}";
            }
        }

        internal static string GetOperandStringStringArgs(Expression exp)
        {
            if (exp is ConstantExpression constExp)
            {
                if (constExp.Type == typeof(string))
                    return $"\"{constExp.Value}\"";
                else
                    return $"{constExp.Value}";
            }
            if (exp is MemberExpression member)
                return GetOperandStringForMember(member);
            if (exp is MethodCallExpression method)
                return $"@{((ConstantExpression)method.Arguments[0]).Value}";
            if (exp is UnaryExpression unary)
                return GetOperandString(unary.Operand);
            if (exp is BinaryExpression binExpression)
                return ParseBinaryExpression(binExpression);
            return string.Empty;
        }

        internal static string GetOperandStringForQueryArgs(Expression exp)
        {
            if (exp is ConstantExpression constExp)
            {
                return $"{constExp.Value}";
            }
            if (exp is MemberExpression member)
                return GetOperandStringForMember(member, true);
            if (exp is MethodCallExpression method)
                return TranslateContainsStandardQuerySyntax(method);
            if (exp is UnaryExpression unary)
                return GetOperandStringForQueryArgs(unary.Operand);            
            throw new ArgumentException("Unrecognized Expression type");
        }

        internal static string MapMethodName(string methodName) => methodName switch
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
            _ => ""
        };

        internal static string ParseMathMethod(MethodCallExpression exp, string methodName)
        {
            var sb = new StringBuilder();
            var args = new List<string>();
            sb.Append($"{methodName}(");
            sb.Append(GetOperandString(exp.Arguments[0]));
            sb.Append(")");
            return sb.ToString();
        }

        internal static string ParseMethod(MethodCallExpression exp, string methodName)
        {
            var sb = new StringBuilder();
            var args = new List<string>();
            sb.Append($"{methodName}(");
            if (exp.Object != null)
            {
                args.Add(GetOperandStringStringArgs(exp.Object));

            }
            foreach (var arg in exp.Arguments)
            {
                args.Add(GetOperandStringStringArgs(arg));
            }
            sb.Append(string.Join(",", args));
            if (methodName == "substr" && args.Count == 2)
                sb.Append(",-1");
            sb.Append(")");
            return sb.ToString();
        }

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

        internal static string ParseFormatMethod(MethodCallExpression exp)
        {
            var pattern = "\\{(\\d+|)\\}";
            var args = new List<string>();
            var sb = new StringBuilder();
            sb.Append("format(");
            var formatStringExpression = exp.Arguments[0];
            var formatArgs = new List<string>();
            string formatString = string.Empty;
            if (formatStringExpression is ConstantExpression constantFormattedExpression)
            {
                formatString = constantFormattedExpression.Value.ToString();
                args.Add($"\"{Regex.Replace(formatString, pattern, "%s")}\"");
            }
            else if (formatStringExpression is MemberExpression member
                && member.Expression is ConstantExpression constInnerExpression)
            {
                formatString = (string)GetValue(member.Member, constInnerExpression.Value);
                args.Add($"\"{Regex.Replace(formatString, pattern, "%s")}\"");
            }

            for (var i = 1; i < exp.Arguments.Count; i++)
            {
                formatArgs.Add(GetOperandStringStringArgs(exp.Arguments[i]));
            }



            var matches = Regex.Matches(formatString, pattern);
            foreach (var m in matches)
            {
                var match = (Match)m;
                var subStr = match.Value.Substring(1, match.Length - 2);
                var matchIndex = int.Parse(subStr);
                args.Add(formatArgs[matchIndex]);
            }
            sb.Append(string.Join(",", args));
            sb.Append(")");
            return sb.ToString();
        }

        internal static string ParseSplitMethod(MethodCallExpression exp)
        {
            var args = new List<string>();
            var sb = new StringBuilder();
            sb.Append("split(");
            args.Add(GetOperandStringStringArgs(exp.Object));
            var arg = exp.Arguments[0];
            if (arg.Type == typeof(string) || arg.Type == typeof(char))
            {
                args.Add($"\"{GetOperandStringStringArgs(arg)}\"");
            }
            else
            {
                if (arg is MemberExpression member
                    && member.Expression is ConstantExpression constExp)
                {
                    var innerArgList = new List<string>();
                    if (member.Type == typeof(char[]))
                    {
                        var charArr = (char[])GetValue(member.Member, constExp.Value);
                        foreach (var c in charArr)
                        {
                            innerArgList.Add(c.ToString());
                        }
                    }
                    else if (member.Type == typeof(string[]))
                    {
                        var stringArr = (string[])GetValue(member.Member, constExp.Value);
                        foreach (var s in stringArr)
                        {
                            innerArgList.Add(s);
                        }
                    }
                    args.Add($"\"{string.Join(",", innerArgList)}\"");
                }
                else if (arg is NewArrayExpression arrayExpression)
                {
                    var innerArgList = new List<string>();
                    foreach (var item in arrayExpression.Expressions)
                    {
                        if (item is ConstantExpression constant)
                        {
                            innerArgList.Add(constant.Value.ToString());
                        }
                    }
                    args.Add($"\"{string.Join(",", innerArgList)}\"");
                }
            }

            sb.Append(string.Join(",", args));
            sb.Append(")");
            return sb.ToString();
        }

        internal static string GetOperationName(string methodName)
        {
            return methodName switch
            {
                nameof(string.ToUpper) => "upper",
                nameof(string.ToLower) => "lower",
                nameof(string.Contains) => "contains",
                nameof(string.StartsWith) => "startswith",
                nameof(string.Substring) => "substr",
                nameof(string.Format) => "format",
                nameof(string.Split) => "split",
                _ => ""
            };
        }

        internal static string GetOperatorFromNodeType(ExpressionType type)
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
                _ => ""
            };
        }

        internal static string ParseBinaryExpression(BinaryExpression rootBinaryExpression)
        {
            var operationStack = new Stack<string>();
            var binExpressions = SplitBinaryExpression(rootBinaryExpression);
            foreach (var expression in binExpressions)
            {
                var right = GetOperandString(expression.Right);
                var left = GetOperandString(expression.Left);
                operationStack.Push(right);
                operationStack.Push(GetOperatorFromNodeType(expression.NodeType));
                if (!string.IsNullOrEmpty(left))
                    operationStack.Push(left);
            }
            return string.Join(" ", operationStack);
        }

        internal static IEnumerable<BinaryExpression> SplitBinaryExpression(BinaryExpression? exp)
        {
            var list = new List<BinaryExpression>();
            do
            {
                list.Add(exp);                
                if (exp.Left is UnaryExpression unExp)
                    exp = unExp.Operand as BinaryExpression;
                else if (exp.Left is BinaryExpression)
                    exp = exp.Left as BinaryExpression;
                else
                    exp = null;
            } while (exp != null);
            return list;
        }

        internal static string TranslateMethodExpressions(MethodCallExpression exp)
        {
            return exp.Method.Name switch
            {
                "Contains" => TranslateContainsStandardQuerySyntax(exp),
                _ => throw new ArgumentException($"Unrecognized method for query translation:{exp.Method.Name}")
            };
        }

        internal static string TranslateContainsStandardQuerySyntax(MethodCallExpression exp)
        {
            if(exp.Object is MemberExpression member)
            {
                var memberName = GetOperandStringForMember(member, true);
                var literal = GetOperandStringForQueryArgs(exp.Arguments[0]);
                return $"{memberName}:{literal}";
            }
            else
            {
                throw new ArgumentException("String that Contains is called on must be a member of an indexed class");
            }
        }

        internal static RedisGeoFilter TranslateGeoFilter(MethodCallExpression exp)
        {            
            var memberOperand = GetOperandString(exp.Arguments[1]).Substring(1);
            var longitude = (double)((ConstantExpression)exp.Arguments[2]).Value;
            var latitude = (double)((ConstantExpression)exp.Arguments[3]).Value;
            var radius = (double)((ConstantExpression)exp.Arguments[4]).Value;
            var unit = (GeoLocDistanceUnit)((ConstantExpression)exp.Arguments[5]).Value;
            return new RedisGeoFilter(memberOperand, longitude, latitude, radius, unit);
        }
    }
}
