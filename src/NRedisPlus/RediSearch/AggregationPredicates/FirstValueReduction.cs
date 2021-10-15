using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NRedisPlus.Model;

namespace NRedisPlus.RediSearch
{
    public class FirstValueReduction : Reduction
    {        
        private string _returnArg;
        private int _numArgs = 1;
        private string _sortArg = string.Empty;
        private SortDirection? _direction;
        public FirstValueReduction(MethodCallExpression exp) : base(ReduceFunction.FIRST_VALUE) 
        {
            _returnArg = ExpressionParserUtilities.GetOperandString(exp.Arguments[1]);
            if (exp.Arguments.Count > 2)
            {
                _sortArg = ExpressionParserUtilities.GetOperandString(exp.Arguments[2]);
                _numArgs+=2;
            }
            if (exp.Arguments.Count > 3)
            {
                var dir = ExpressionParserUtilities.GetOperandString(exp.Arguments[3]);
                SortDirection enumeratedDir;
                if (Enum.TryParse(dir, out enumeratedDir))
                {
                    _direction = enumeratedDir;
                    _numArgs++;
                }                    
            }
        }
        public override string ResultName => $"{_returnArg.Substring(1)}_{_function}";

        public override string[] Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(_function.ToString());
            ret.Add(_numArgs.ToString());
            ret.Add(_returnArg);
            if (!string.IsNullOrEmpty(_sortArg))
            {
                ret.Add("BY");
                ret.Add($"@{_sortArg}");

                if (_direction != null)
                {
                    if (_direction == SortDirection.Ascending)
                        ret.Add("ASC");
                    else
                        ret.Add("DESC");
                }
                    
            }
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
