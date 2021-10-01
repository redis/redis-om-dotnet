using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{
    public class SortedSetEntry
    {
        public double Score { get; set; }
        public string Member { get; set; } = string.Empty;
        
        public static implicit operator string[](SortedSetEntry e)=>new string[] { e.Score.ToString(), e.Member };        
        public static string[] BuildRequestArray(SortedSetEntry[] e)        
        {
            var ret = new List<string>();
            foreach(var entry in e)
            {
                ret.AddRange((string[])entry);
            }
            return ret.ToArray();
                
        }
    }
}
