namespace NRedisPlus.RediSearch
{
    public class LimitPredicate : IAggregationPredicate
    {
        public long Offset { get; set; } = 0;
        public long Count { get; set; } = 100;        
        public string[] Serialize()
        {
            return new[] { "LIMIT", Offset.ToString(), Count.ToString() };
        }
    }
}
