using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class RedisFilter : QueryOption
    {
        public string FieldName { get; set; } = string.Empty;
        public int Min { get; set; }
        public int Max { get; set; }

        public override string[] QueryText
        {
            get 
            {
                var ret = new List<string>();
                ret.Add("FILTER");
                ret.Add(FieldName);
                ret.Add(Min.ToString());
                ret.Add(Max.ToString());
                return ret.ToArray();
            }
        }
    }
}
