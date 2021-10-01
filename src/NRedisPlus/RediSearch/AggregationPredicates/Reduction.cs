namespace NRedisPlus.RediSearch
{
    public abstract class Reduction : IAggregationPredicate
    {
        protected ReduceFunction _function;
        public abstract string ResultName { get; }
        public Reduction(ReduceFunction function)
        {
            _function = function;
        }

        public abstract string[] Serialize();
    }
}
