using System;
using System.Collections.Generic;
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

        [Fact]
        public void TestHashTypeWithArrayOfPrimitives()
        {
            var obj = new HashTypeWithPrimitiveArray()
            {
                Name = "foo",
                Ints = new[] { 1, 2, 3, 4, 5 },
                Bools = new[] { true,false,true },
                Shorts = new short[] { 1, 2, 3, 4, 5 },
                Bytes = new byte[] { 1, 2, 3, 4, 5 },
                SBytes = new sbyte[] { 1, 2, 3, 4, 5 },
                UShorts = new ushort[] { 1, 2, 3, 4, 5 },
                UInts = new uint[] { 1, 2, 3, 4, 5 },
                Longs = new long[] { 1, 2, 3, 4, 5 },
                ULongs = new ulong[] { 1, 2, 3, 4, 5 },
                Chars = new char[] { 'a','b','c','d','e' },
                Doubles = new double[] { 1, 2, 3, 4, 5 },
                Floats = new float[] { 1, 2, 3, 4, 5 },
                IntList = new List<int>(){1, 2, 3, 4, 5}
            };

            var id = _connection.Set(obj);

            var res = _connection.Get<HashTypeWithPrimitiveArray>(id);
            Assert.Equal(obj.Name,res.Name);
            Assert.Equal(obj.Bools, res.Bools);
            Assert.Equal(obj.Shorts, res.Shorts);
            Assert.Equal(obj.Bytes, res.Bytes);
            Assert.Equal(obj.SBytes, res.SBytes);
            Assert.Equal(obj.UShorts, res.UShorts);
            Assert.Equal(obj.Ints, res.Ints);
            Assert.Equal(obj.UInts, res.UInts);
            Assert.Equal(obj.Longs, res.Longs);
            Assert.Equal(obj.ULongs, res.ULongs);
            Assert.Equal(obj.Chars, res.Chars);
            Assert.Equal(obj.Doubles, res.Doubles);
            Assert.Equal(obj.Floats, res.Floats);
            Assert.Equal(obj.IntList, res.IntList);
        }
    }
}