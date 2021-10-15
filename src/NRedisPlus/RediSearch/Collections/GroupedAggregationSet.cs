using System.Linq.Expressions;
using NRedisPlus.Contracts;

namespace NRedisPlus.RediSearch
{
    public class GroupedAggregationSet<T> : RedisAggregationSet<T>
    {
        public GroupedAggregationSet(IRedisConnection connection, bool useCursor = false) : base(connection, useCursor)
        {            
        }

        internal GroupedAggregationSet(RedisQueryProvider provider, bool useCursor = false) : base(provider, useCursor)
        {            
        }

        //internal GroupedAggregationSet(RedisQueryProvider provider, Expression expression, bool useCursor = false) : base(provider, expression, useCursor)
        //{            
        //}

        internal GroupedAggregationSet(RedisAggregationSet<T> source, Expression expression) : base(source, expression)
        {
        }
    }
}
