using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class SearchLimit : QueryOption
    {
        public int Offset { get; set; } = 0;
        public int Number { get; set; } = 10;
        public override string[] QueryText { 
            get {
                return new string[]
                {
                    "LIMIT",
                    Offset.ToString(),
                    Number.ToString()
                };                
            } 
        }
    }
}
