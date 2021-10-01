using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class SingleArgumentReduction : Reduction
    {        
        private string _arg { get; set; }

        public override string ResultName => $"{_arg}_{_function}";

        public SingleArgumentReduction(ReduceFunction function, string arg) : base(function)
        {            
            _arg = arg;
        }

        public override string[] Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(_function.ToString());
            ret.Add("1");
            ret.Add($"@{_arg}");
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
