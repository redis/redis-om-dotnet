using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class ObjectWithUserDefinedId
    {
        [RedisIdField] public string Id { get; set; }
        public string Name { get; set; }
    }
}