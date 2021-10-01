using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class ZeroArgumentReduction : Reduction
    {
        public override string ResultName => $"{_function}";
        public ZeroArgumentReduction(ReduceFunction function) : base(function) { }

        public override string[] Serialize()
        {
            var ret = new List<string>();
            ret.Add("REDUCE");
            ret.Add(_function.ToString());
            ret.Add("0");
            ret.Add("AS");
            ret.Add(ResultName);
            return ret.ToArray();
        }
    }
}
