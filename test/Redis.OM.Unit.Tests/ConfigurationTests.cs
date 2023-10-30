using System.Linq;
using System.Net;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void TestEmptyConfiguration()
        {
            var options = RedisUriParser.ParseConfigFromUri("");

            var endpoint = (DnsEndPoint)options.EndPoints.First();
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(6379, endpoint.Port);
            Assert.False(options.Ssl);
        }
        
        [Fact]
        public void TestBasicConfigurationNoPort()
        {
            var options = RedisUriParser.ParseConfigFromUri("redis://localhost");

            var endpoint = (DnsEndPoint)options.EndPoints.First();
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(6379, endpoint.Port);
            Assert.False(options.Ssl);
        }
        
        [Fact]
        public void TestBasicConfigurationWithPassword()
        {
            var options = RedisUriParser.ParseConfigFromUri("redis://:password@localhost:6379");

            var endpoint = (DnsEndPoint)options.EndPoints.First();
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(6379, endpoint.Port);
            Assert.Equal("password", options.Password);
            Assert.False(options.Ssl);
        }
        
        [Fact]
        public void TestBasicConfigurationWithUsernameAndPassword()
        {
            var options = RedisUriParser.ParseConfigFromUri("redis://username:password@localhost:6379");

            var endpoint = (DnsEndPoint)options.EndPoints.First();
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(6379, endpoint.Port);
            Assert.Equal("password", options.Password);
            Assert.Equal("username", options.User);
            Assert.False(options.Ssl);
        }
        
        [Fact]
        public void TestBasicConfigurationWithEncodedUsernameAndPassword()
        {
            var options = RedisUriParser.ParseConfigFromUri("redis://foo%23bar:p%40ssword@localhost:6379");

            var endpoint = (DnsEndPoint)options.EndPoints.First();
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(6379, endpoint.Port);
            Assert.Equal("p@ssword", options.Password);
            Assert.Equal("foo#bar", options.User);
            Assert.False(options.Ssl);
        }
        
        [Fact]
        public void TestWithTls()
        {
            var options = RedisUriParser.ParseConfigFromUri("rediss://username:password@localhost:6379");

            var endpoint = (DnsEndPoint)options.EndPoints.First();
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(6379, endpoint.Port);
            Assert.Equal("password", options.Password);
            Assert.Equal("username", options.User);
            Assert.True(options.Ssl);
        }
        
        [Fact]
        public void TestWithTimeout()
        {
            var options = RedisUriParser.ParseConfigFromUri("rediss://username:password@localhost:6379?timeout=1000");
            Assert.Equal(1000, options.AsyncTimeout);
            Assert.Equal(1000, options.ConnectTimeout);
            Assert.Equal(1000, options.SyncTimeout);
        }

        [Fact]
        public void TestWithSpecifiedDatabase()
        {
            var options = RedisUriParser.ParseConfigFromUri("rediss://username:password@localhost:6379/4?timeout=1000");
            Assert.Equal(4, options.DefaultDatabase);
        }
        
        [Fact]
        public void TestWithSpecifiedClientName()
        {
            var options = RedisUriParser.ParseConfigFromUri("rediss://username:password@localhost:6379/4?timeout=1000&clientname=bob");
            Assert.Equal("bob", options.ClientName);
        }
        
        [Fact]
        public void TestWithMultipleEndpoints()
        {
            var options = RedisUriParser.ParseConfigFromUri("rediss://username:password@localhost:6379?endpoint=notSoLocalHost:6379&endpoint=reallyNotSoLocalHost:6379");
            Assert.True(options.EndPoints.Any(x =>x is DnsEndPoint endpoint && endpoint.Host == "notSoLocalHost" && endpoint.Port == 6379));
            Assert.True(options.EndPoints.Any(x =>x is DnsEndPoint endpoint && endpoint.Host == "reallyNotSoLocalHost" && endpoint.Port == 6379));
        }
    }
}