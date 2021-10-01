using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public abstract class QueryOption
    {
        public abstract string[] QueryText { get; }
    }
}
