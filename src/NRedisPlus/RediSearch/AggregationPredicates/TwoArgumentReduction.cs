using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class TwoArgumentReduction : Reduction
    {        
        private string _arg1;
        private string _arg2;

        public TwoArgumentReduction(ReduceFunction func, MethodCallExpression expression) : base(func)
        {            
            _arg1 = ExpressionParserUtilities.GetOperandString(expression.Arguments[1]);
            _arg2 = ExpressionParserUtilities.GetOperandString(expression.Arguments[2]);            
        }

        public override string ResultName => $"{_arg1.Substring(1)}_{_function}_{_arg2}";

        public override string[] Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(_function.ToString());
            ret.Add("2");
            ret.Add(_arg1);
            ret.Add(_arg2);
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
