using Xunit;
using System;
using StackExchange.Redis;
using System.Linq;
using System.IO;
using Redis.OM;
using Redis.OM.Modeling;
using System.Threading;
using System.Threading.Tasks;
using Redis.OM.Unit.Tests.RediSearchTests;

namespace Redis.OM.Unit.Tests
{
    public class CoreTests
    {
        [Document(IndexName = "jsonexample-idx", StorageType = StorageType.Json)]
        public class ModelExampleJson
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }


        [Fact]
        public void SimpleConnectionTest()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            connection.Set("x", "value");
            var result = connection.Get("x");
            Assert.Equal("value", result);
        }

        [Fact]
        public void StackExchangeConnectionTest()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            connection.Set("x", "value");
            var result = connection.Get("x");
            Assert.Equal("value", result);
        }

        [Fact]
        public void SimpleGetSetTest()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            connection.Set("x", "value");
            var result = connection.Get("x");
            Assert.Equal("value", result);
        }

        [Fact]
        public async Task ExpireFractionalMillisecondAsync()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            var jsonObjWithExpire = new BasicJsonObject { Name = "JsonWithExpire" };
            var key = await connection.SetAsync(jsonObjWithExpire, TimeSpan.FromMilliseconds(5000.5));
            var ttl = (long)await connection.ExecuteAsync("PTTL", key);
            Assert.True(ttl <= 5000.5);
            Assert.True(ttl >= 1000);
        }

        [Fact]
        public void ExpireFractionalMillisecond()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            var jsonObjWithExpire = new BasicJsonObject { Name = "JsonWithExpire" };
            var key = connection.Set(jsonObjWithExpire, TimeSpan.FromMilliseconds(5000.5));
            var ttl = (long)connection.Execute("PTTL", key);
            Assert.True(ttl <= 5000.5);
            Assert.True(ttl >= 1000);
        }

        [Fact]
        public void ExpireTest()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            var jsonObj = new BasicJsonObject { Name = "JsonWithoutExpire" };
            var jsonObjWithExpire = new BasicJsonObject { Name = "JsonWithExpire" };

            var hashObj = new BasicHashObject { Name = "HashWithoutExpire" };
            var hashObjWithExpire = new BasicHashObject { Name = "HashWithExpire" };

            connection.Set(jsonObj);
            connection.Set(jsonObjWithExpire, TimeSpan.FromSeconds(1));

            connection.Set(hashObj);
            connection.Set(hashObjWithExpire, TimeSpan.FromSeconds(1));


            Thread.Sleep(1500);

            var hashNotExpired = connection.HGetAll(hashObj.GetKey(null));
            var hashExpired = connection.HGetAll(hashObjWithExpire.GetKey(null));

            var jsonNotExpired = connection.JsonGet(jsonObj.GetKey(null));
            var jsonExpired = connection.JsonGet(jsonObjWithExpire.GetKey(null));

            Assert.Equal( 2, hashNotExpired.Count);
            Assert.Equal(0, hashExpired.Count);

            Assert.NotEqual("", jsonNotExpired);
            Assert.Equal("", jsonExpired);
        }

        [Fact]
        public async Task ExpireTestAsync()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            var jsonObj = new BasicJsonObject { Name = "JsonWithoutExpire" };
            var jsonObjWithExpire = new BasicJsonObject { Name = "JsonWithExpire" };

            var hashObj = new BasicHashObject { Name = "HashWithoutExpire" };
            var hashObjWithExpire = new BasicHashObject { Name = "HashWithExpire" };

            await connection.SetAsync(jsonObj);
            await connection.SetAsync(jsonObjWithExpire, TimeSpan.FromSeconds(1));

            await connection.SetAsync(hashObj);
            await connection.SetAsync(hashObjWithExpire, TimeSpan.FromSeconds(1));

            Thread.Sleep(1500);

            var hashNotExpired = await connection.HGetAllAsync(hashObj.GetKey(null));
            var hashExpired = await connection.HGetAllAsync(hashObjWithExpire.GetKey(null));

            var jsonNotExpired = await connection.JsonGetAsync(jsonObj.GetKey(null));
            var jsonExpired = await connection.JsonGetAsync(jsonObjWithExpire.GetKey(null));

            Assert.Equal(2, hashNotExpired.Count);
            Assert.Equal(0, hashExpired.Count);

            Assert.NotEqual("",jsonNotExpired);
            Assert.Equal("", jsonExpired);
        }

        [Fact]
        public void SimpleJsonTest()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            var obj = new ModelExampleJson { Name = "Steve", Age = 32 };
            connection.JsonSet("test-json", ".", obj);
            var reconsitutedObject = connection.JsonGet<ModelExampleJson>("test-json");
            Assert.Equal("Steve", reconsitutedObject.Name);
            Assert.Equal(32, reconsitutedObject.Age);
        }

        [Fact]
        public void SimpleJsonTestSE()
        {
            var obj = new ModelExampleJson { Name = "Steve", Age = 32 };
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            connection.JsonSet("test-json", ".", obj);
            var reconsitutedObject = connection.JsonGet<ModelExampleJson>("test-json");
            Assert.Equal("Steve", reconsitutedObject.Name);
            Assert.Equal(32, reconsitutedObject.Age);
        }

        [Fact]
        public void CommandTest()
        {
            var hostInfo = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost:6379";
            var provider = new RedisConnectionProvider($"redis://{hostInfo}");
            var connection = provider.Connection;
            var res = connection.Execute("COMMAND");
            var stream = File.AppendText("commands.json");
            stream.WriteLine("[");
            foreach (var item in res.ToArray())
            {
                stream.WriteLine("{");
                var args = item.ToArray();
                var name = (string)args[0];
                var arity = (long)args[1];
                var flags = args[2].ToArray().Select(x => (string)x).ToArray();
                var firstKeyPos = (long)args[3];
                var lastKeyPos = (long)args[4];
                var stepCount = (long)args[5];
                stream.WriteLine($"\"name\": \"{name}\",");
                stream.WriteLine($"\"arity\": {arity},");
                var flagsString = string.Join(", ", flags.Select(x => $"\"{x}\""));
                stream.WriteLine($"\"flags\": [{flagsString}],");
                stream.WriteLine($"\"first_key_pos\": {firstKeyPos},");
                stream.WriteLine($"\"last_key_pos\": {lastKeyPos},");
                stream.WriteLine($"\"step_count\": {stepCount}");
                stream.WriteLine("},");
            }
            stream.WriteLine("]");
            stream.Flush();
        }
    }
}
