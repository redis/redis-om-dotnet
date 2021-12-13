using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class ObjectWithIntegerId
    {
        [RedisIdField] public int Id { get; set; }
    }
}