using System.Linq;
using System.Linq.Expressions;

namespace Redis.OM
{
    /// <summary>
    /// Rewrites the <c>System.MemoryExtensions.Contains(ReadOnlySpan&lt;T&gt;, T)</c> calls that the C# 14 /
    /// .NET 10 compiler now prefers for array (and other span-convertible) <c>Contains</c> invocations back
    /// into <see cref="System.Linq.Enumerable.Contains{TSource}(System.Collections.Generic.IEnumerable{TSource}, TSource)"/>
    /// calls so the rest of the query translator (which only understands the <c>Enumerable.Contains</c> shape)
    /// can handle them. This keeps working regardless of the framework Redis OM itself targets, because it
    /// only inspects the consumer's expression tree.
    /// </summary>
    internal class SpanToEnumerableVisitor : ExpressionVisitor
    {
        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Contains"
                && node.Method.DeclaringType?.FullName == "System.MemoryExtensions"
                && node.Arguments.Count == 2)
            {
                var source = UnwrapSpanSource(node.Arguments[0]);
                if (source != null)
                {
                    var itemToFind = node.Arguments[1];
                    var enumerableContains = typeof(Enumerable)
                        .GetMethods()
                        .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(itemToFind.Type);

                    return Expression.Call(null, enumerableContains, Visit(source), itemToFind);
                }
            }

            return base.VisitMethodCall(node);
        }

        private static Expression? UnwrapSpanSource(Expression spanArgument)
        {
            // The array/list -> ReadOnlySpan<T> conversion is emitted as an op_Implicit method call
            // (Expression.Convert to a ref struct is not legal in an expression tree).
            Expression? source = spanArgument switch
            {
                MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } implicitCall => implicitCall.Arguments[0],
                UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary => unary.Operand,
                _ => null,
            };

            if (source is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } innerConvert)
            {
                source = innerConvert.Operand;
            }

            return source;
        }
    }
}
