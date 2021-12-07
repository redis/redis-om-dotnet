using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(IdGenerationStrategyName = nameof(StaticIncrementStrategy))]
    public class ObjectWithCustomIdGenerationStrategy
    {
        [RedisIdField] public string Id { get; set; }
    }
}