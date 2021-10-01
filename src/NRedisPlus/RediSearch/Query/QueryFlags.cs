using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public enum QueryFlags
    {
        NOCONTENT = 1,
        VERBATIM = 2,
        NOSTOPWORDS = 4,
        WITHSCORES = 8,
        WITHPAYLOADS = 16,
        WITHSORTKEYS = 32
    }
}
