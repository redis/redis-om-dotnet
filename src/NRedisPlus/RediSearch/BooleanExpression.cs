using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public abstract class BooleanExpression
    {
        protected LambdaExpression _expression;
        protected BooleanExpression(LambdaExpression expression)
        {
            _expression = expression;
        }        

        protected abstract string GetOperatorSymbol(ExpressionType expressionType);
        protected abstract void ValidateAndPushOperand(Expression expression, Stack<string> stack);

        protected virtual void SplitBinaryExpression(BinaryExpression expression, Stack<string> stack)
        {
            if (expression.Right is BinaryExpression rightBinary)
            {
                SplitBinaryExpression(rightBinary, stack);
            }
            else
            {
                ValidateAndPushOperand(expression.Right, stack);
            }
            stack.Push(GetOperatorSymbol(expression.NodeType));
            if (expression.Left is BinaryExpression leftBinary)
            {
                SplitBinaryExpression(leftBinary, stack);
            }
            else
            {
                ValidateAndPushOperand(expression.Left, stack);
            }
        }

        protected Stack<string> SplitExpression()
        {
            var ret = new Stack<string>();
            
            if (_expression.Body is BinaryExpression binExpression)
            {
                SplitBinaryExpression(binExpression, ret);
            }
            else
            {
                ValidateAndPushOperand(_expression.Body, ret);
            }
            return ret;
        }
    }
}
