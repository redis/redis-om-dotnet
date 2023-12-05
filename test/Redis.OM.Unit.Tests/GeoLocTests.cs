using System.Collections.Generic;
using System.Text.Json;
using Redis.OM.Modeling;
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
            Assert.Equal("Foo", basicType.Name);
            Assert.Equal(32.5, basicType.Location.Longitude);
            Assert.Equal(22.4, basicType.Location.Latitude);
        }

        [Fact]
        public void TestParsingFromFormattedHash()
        {
            var hash = new Dictionary<string, RedisReply>
            {
                {"Name", "Foo"},
                {"Location", "32.5,22.4"}
            };

            var basicType = RedisObjectHandler.FromHashSet<BasicTypeWithGeoLoc>(hash);
            Assert.Equal("Foo", basicType.Name);
            Assert.Equal(32.5, basicType.Location.Longitude);
            Assert.Equal(22.4, basicType.Location.Latitude);
        }

        /// <summary>
        /// This test will pass only if parsing of formatted geoloc string values is culture invariant.
        /// </summary>
        [Fact]
        public void TestInvariantCultureParsingFromFormattedHash()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestParsingFromFormattedHash());
        }

        [Theory]
        [InlineData("en-DE")]
        [InlineData("it-IT")]
        public void TestToStringInOtherCultures(string lcid)
        {
            Helper.RunTestUnderDifferentCulture(lcid, o =>
            {
                var geoLoc = new GeoLoc(45.2, 11.9);
                var geoLocStr = geoLoc.ToString();
                Assert.Equal("45.2,11.9", geoLocStr);
            });
        }
    }
}