using System;
using System.Linq;
using System.Linq.Expressions;

namespace Redis.OM
{
    /// <summary>
    /// Expresison visitor that converts Span.Contains calls to Enumerable.Contains calls.
    /// </summary>
    public class SpanToEnumerableVisitor : ExpressionVisitor
    {
        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Contains" && node.Method.DeclaringType == typeof(MemoryExtensions))
            {
                if (node.Arguments[0] is MethodCallExpression implicitCall && implicitCall.Method.Name == "op_Implicit")
                {
                    var source = implicitCall.Arguments[0];

                    if (source is UnaryExpression unary && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
                    {
                        source = unary.Operand;
                    }

                    var itemToFind = node.Arguments[1];
                    var enumerableContains = typeof(Enumerable)
                        .GetMethods()
                        .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(itemToFind.Type);

                    return Expression.Call(null, enumerableContains, source, itemToFind);
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}