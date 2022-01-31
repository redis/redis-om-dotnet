using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(IdGenerationStrategyName = nameof(Uuid4IdGenerationStrategy))]
    public class ObjectWithGuidId
    {
        [RedisIdField]public Guid Id { get; set; }
    }
}