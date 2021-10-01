using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public enum ReduceFunction
    {
        COUNT,
        COUNT_DISTINCT,
        COUNT_DISTINCTISH,
        SUM,
        MIN,
        MAX,
        AVG,
        STDDEV,
        QUANTILE,
        TOLIST,
        FIRST_VALUE,
        RANDOM_SAMPLE
    }
}
