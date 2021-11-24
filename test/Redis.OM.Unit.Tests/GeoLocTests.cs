using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    public class GeoLocTests
    {
        [Fact]
        public void TestParsingFromJson()
        {
            var str = "{\"Name\":\"Foo\", \"Location\":{\"Longitude\":32.5,\"Latitude\":22.4}}";
            var basicType = JsonSerializer.Deserialize<BasicTypeWithGeoLoc>(str);
            Assert.Equal("Foo",basicType.Name);
            Assert.Equal(32.5, basicType.Location.Longitude);
            Assert.Equal(22.4, basicType.Location.Latitude);
        }

        [Fact]
        public void TestParsingFromFormattedHash()
        {
            var hash = new Dictionary<string, string>
            {
                {"Name", "Foo"},
                {"Location", "32.5,22.4"}
            };

            var basicType = RedisObjectHandler.FromHashSet<BasicTypeWithGeoLoc>(hash);
            Assert.Equal("Foo",basicType.Name);
            Assert.Equal(32.5, basicType.Location.Longitude);
            Assert.Equal(22.4, basicType.Location.Latitude);
        }
    }
}