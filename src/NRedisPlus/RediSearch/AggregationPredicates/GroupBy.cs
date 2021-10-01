using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class GroupBy : IAggregationPredicate
    {
        public string[] Properties { get; set; }        
        public GroupBy(string[] properties)
        {
            Properties = properties;            
        }
        
        public string[] Serialize()
        {
            var ret = new List<string>();
            ret.Add("GROUPBY");
            ret.Add(Properties.Length.ToString());
            foreach(var property in Properties)
            {
                ret.Add($"@{property}");
            }            
            return ret.ToArray();
        }
    }
}
