using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class FilterPredict : Apply, IAggregationPredicate
    {


        public FilterPredict(Expression expression) : base(expression, string.Empty)
        {
            
        }       

        public new string[] Serialize()
        {
            var list = new List<string> { "FILTER" };            
            if (_expression is BinaryExpression rootBinExpression)
            {
                list.Add(ExpressionParserUtilities.ParseBinaryExpression(rootBinExpression));
            }
            else if (_expression is MethodCallExpression method)
            {
                list.Add(ExpressionParserUtilities.GetOperandString(method));
            }
            else
            {
                list.Add(ExpressionParserUtilities.GetOperandString(_expression));
            }
            return list.ToArray();
        }
    }
}
