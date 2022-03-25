using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
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
            var hash = new Dictionary<string, string>
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
        /// It will fail for example when the process runs in an environment having a culture that use a comma (",") or any other char different from dot (".")
        /// as number decimal separator (e.g. it-IT culture).
        /// </summary>
        [Fact]
        public void TestInvariantCultureParsingFromFormattedHash()
        {
            // store original process culture objects
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            var currentUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                var differentCulture = new System.Globalization.CultureInfo("it-IT");

                Assert.NotEqual(".", differentCulture.NumberFormat.NumberDecimalSeparator);

                // set a different culture for the current thread
                Thread.CurrentThread.CurrentCulture = differentCulture;
                Thread.CurrentThread.CurrentUICulture = differentCulture;

                var hash = new Dictionary<string, string>
                {
                    {"Name", "Foo"},
                    {"Location", "32.5,22.4"}
                };

                var basicType = RedisObjectHandler.FromHashSet<BasicTypeWithGeoLoc>(hash);
                Assert.Equal("Foo", basicType.Name);
                Assert.Equal(32.5, basicType.Location.Longitude);
                Assert.Equal(22.4, basicType.Location.Latitude);
            }
            finally
            {
                // restore original process culture objects
                Thread.CurrentThread.CurrentCulture = currentCulture;
                Thread.CurrentThread.CurrentUICulture = currentUICulture;
            }
        }
    }
}