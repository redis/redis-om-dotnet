using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class ObjectWithStandardId
    {
        [RedisIdField] public string Id { get; set; }
    }
}