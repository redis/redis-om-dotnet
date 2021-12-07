using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    [Collection("Redis")]
    public class SerializationTests
    {
        private readonly IRedisConnection _connection;

        public SerializationTests(RedisSetup setup)
        {
            _connection = setup.Connection;
        }
        
        [Fact]
        public void TestUserDefinedId()
        {
            var obj = new ObjectWithUserDefinedId {Id = "5", Name = "Steve"};
            var id = _connection.Set(obj);
            Assert.Equal("5", id.Split(':')[1]);
        }

        [Fact]
        public void TestUserDefinedStrategy()
        {
            DocumentAttribute.RegisterIdGenerationStrategy(nameof(StaticIncrementStrategy), new StaticIncrementStrategy());
            var obj = new ObjectWithCustomIdGenerationStrategy();
            var id = _connection.Set(obj);
            Assert.Equal("1", obj.Id);
            Assert.Equal("1", id.Split(":")[1]);
        }
    }
}