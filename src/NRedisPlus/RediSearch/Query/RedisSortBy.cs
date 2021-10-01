using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class RedisSortBy : QueryOption
    {
        public string Field { get; set; } = string.Empty;
        public SortDirection Direction { get; set; }
        public override string[] QueryText { 
            get 
            {
                var dir = Direction == SortDirection.Ascending ? "ASC" : "DESC";
                return new string[] 
                {
                    "SORTBY",
                    Field,
                    dir
                };                
            } 
        }
    }
}
