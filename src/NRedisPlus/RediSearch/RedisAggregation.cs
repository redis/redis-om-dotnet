using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public class RedisAggregation
    {
        public string IndexName { get; set; }
        public QueryPredicate Query { get; set; } = new QueryPredicate();
        public LimitPredicate? Limit { get; set; }
        public Stack<IAggregationPredicate> Predicates { get; set; } = new Stack<IAggregationPredicate>();        

        public RedisAggregation(string indexName)
        {
            IndexName = indexName;
        }

        public string [] Serialize() 
        {
            var ret = new List<string>() { IndexName};
            ret.AddRange(Query.Serialize());
            foreach(var predicate in Predicates)
            {   
                ret.AddRange(predicate.Serialize());
            }
            if(Limit != null)
                ret.AddRange(Limit.Serialize());            
            return ret.ToArray();
        }
    }
}
