using System;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Unit.Tests.RediSearchTests;
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
            Assert.Equal("0", obj.Id);
            Assert.Equal("0", id.Split(":")[1]);
            
            var obj2 = new ObjectWithCustomIdGenerationStrategy();
            var id2 = _connection.Set(obj2);
            Assert.Equal("1", obj2.Id);
            Assert.Equal("1", id2.Split(":")[1]);
        }

        [Fact]
        public void TestUserSetIntId()
        {
            var obj = new ObjectWithIntegerId() {Id = 5};
            var id = _connection.Set(obj);
            Assert.Equal(5, obj.Id);
            Assert.Equal("5", id.Split(":")[1]);
        }

        [Fact]
        public void TestStandardIdGeneration()
        {
            var obj = new ObjectWithStandardId();
            var id = _connection.Set(obj);
            var ulid = Ulid.Parse(id.Split(":")[1]);
            Assert.Equal(ulid.ToString(),obj.Id);
        }
        [Fact]
        public void TestDateTimeOffset()
        {
            var obj = new ObjectWithDateTimeOffset
            {
                Offset = DateTimeOffset.Now
            };

            var id = _connection.Set(obj);

            var alsoObj = _connection.Get<ObjectWithDateTimeOffset>(id);
            Assert.Equal(obj.Offset, alsoObj.Offset);
        }

        [Fact]
        public void TestNotDefaultUlid()
        {
            var obj = new ObjectWithUlidId();
            var id = _connection.Set(obj);
            var ulid = Ulid.Parse(id.Split(":")[1]);
            Assert.NotEqual(default(Ulid), ulid);
            Assert.NotEqual(default(Ulid), obj.Id);
        }

        [Fact]
        public void TestExplicitlySetUlit()
        {
            var ulid = Ulid.NewUlid();
            var obj = new ObjectWithUlidId() {Id = ulid};
            var key = _connection.Set(obj);
            var keyUlid = Ulid.Parse(key.Split(":")[1]);
            Assert.Equal(ulid, keyUlid);
            Assert.Equal(ulid,obj.Id);
        }

        [Fact]
        public void TestUlidMaterialization()
        {
            var ulid = Ulid.NewUlid();
            var obj = new ObjectWithUlidId() {Id = ulid};
            var key = _connection.Set(obj);
            var reconstitutedObject = _connection.Get<ObjectWithUlidId>(key);
            Assert.Equal(ulid, reconstitutedObject.Id);
        }

        [Fact]
        public void TestNotDefaultGuid()
        {
            var obj = new ObjectWithGuidId();
            var id = _connection.Set(obj);
            var guid = Guid.Parse(id.Split(":")[1]);
            Assert.NotEqual(default, guid);
            Assert.NotEqual(default, obj.Id);
        }

        [Fact]
        public void TestTwoMatchingPrefixObjects()
        {
            var obj = new HashObjectWithTwoPropertiesWithMatchingPrefixes
            {
                Name = "Bob",
                LocationNumber = 10
            };

            var id = _connection.Set(obj);
            _connection.Get<HashObjectWithTwoPropertiesWithMatchingPrefixes>(id);
        }
    }
}