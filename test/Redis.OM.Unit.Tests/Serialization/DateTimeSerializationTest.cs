using System;
using System.Threading;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    [Collection("Redis")]
    public class DateTimeSerializationTest
    {
        private readonly IRedisConnection _connection;

        public DateTimeSerializationTest(RedisSetup setup)
        {
            _connection = setup.Connection;
        }

        [Fact]
        public void TestDateTimeSerialization()
        {
            var time = DateTime.Now;
            var obj = new ObjectWithATimestamp { Name = "Foo", Time = time };
            var objNonNullNullTime = new ObjectWithATimestamp { Name = "bar", Time = time, NullableTime = time };
            var id = _connection.Set(obj);
            var id2 = _connection.Set(objNonNullNullTime);
            var reconstituted = _connection.Get<ObjectWithATimestamp>(id);
            var reconstitutedObj2 = _connection.Get<ObjectWithATimestamp>(id2);
            Assert.Equal(time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), reconstituted.Time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            Assert.Null(reconstituted.NullableTime);
            Assert.Equal(time.ToString("yyyy-MM-ddTHH:mm:ss.fff"), reconstitutedObj2.NullableTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }

        [Fact]
        public void TestJsonDateTimeSerialization()
        {
            var time = DateTime.Now;
            var obj = new JsonObjectWithDateTime { Name = "Foo", Time = time };
            var objNonNullNullTime = new JsonObjectWithDateTime { Name = "bar", Time = time, NullableTime = time };
            var id = _connection.Set(obj);
            var id2 = _connection.Set(objNonNullNullTime);
            var reconstituted = _connection.Get<JsonObjectWithDateTime>(id);
            var reconstitutedObj2 = _connection.Get<JsonObjectWithDateTime>(id2);
            Assert.Equal(time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), reconstituted.Time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            Assert.Null(reconstituted.NullableTime);
            Assert.Equal(time.ToString("yyyy-MM-ddTHH:mm:ss.fff"), reconstitutedObj2.NullableTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
        }
    }
}