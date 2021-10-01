using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NRedisPlus.RediSearch
{
    public class Apply : IAggregationPredicate
    {
        protected Expression _expression;
        public string Alias { get; set; }

        public Apply(Expression expression, string alias)
        {
            _expression = expression;
            Alias = alias;
        }

        public string[] Serialize()
        {
            var list = new List<string>();
            list.Add("APPLY");            
            if(_expression is BinaryExpression rootBinExpression)
            {                
                list.Add(ExpressionParserUtilities.ParseBinaryExpression(rootBinExpression));
            }
            else if(_expression is MethodCallExpression method)
            {
                list.Add(ExpressionParserUtilities.GetOperandString(method));
            }
            else
            {
                list.Add(ExpressionParserUtilities.GetOperandString(_expression));
            }
            list.Add("AS");
            list.Add(Alias);
            return list.ToArray();
        }

        
    }
}
