using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class ReturnFields : QueryOption
    {
        public IEnumerable<string> Fields { get; }

        public ReturnFields(IEnumerable<string> fields)
        {
            Fields = fields;
        }
        public override string[] QueryText 
        {
            get 
            {
                var ret = new List<string> { "RETURN", Fields.Count().ToString() };
                foreach(var field in Fields)
                {
                    ret.Add($"{field}");
                }
                return ret.ToArray();
            }
        }
    }
}
