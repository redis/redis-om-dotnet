using Xunit;
using System;
using StackExchange.Redis;
using System.Linq;
using System.IO;
using Redis.OM;
using Redis.OM.Modeling;
using System.Threading;
using System.Threading.Tasks;

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
        public void ExpireTest()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            JsonPerson JsonObj = new JsonPerson() { Name = "JsonWithoutExpire" };
            JsonPerson JsonObjWithExpire = new JsonPerson() { Name = "JsonWithExpire" };

            HashPerson HashObj = new HashPerson() { Name = "HashWithoutExpire" };
            HashPerson HashObjWithExpire = new HashPerson() { Name = "HashWithExpire" };

            var JsonObjId = connection.Set(JsonObj);
            var JsonObjWithExpireId = connection.Set(JsonObjWithExpire, DateTime.Now.AddMilliseconds(1000));

            var HashObjId = connection.Set(HashObj);
            var HashObjWithExpireId = connection.Set(HashObjWithExpire, DateTime.Now.AddMilliseconds(1000));


            Thread.Sleep(1500);

            var HashNotExpired = connection.HGetAll(HashObj.GetKey());
            var HashExpired = connection.HGetAll(HashObjWithExpire.GetKey());

            var JsonNotExpired = connection.JsonGet(JsonObj.GetKey());
            var JsonExpired = connection.JsonGet(JsonObjWithExpire.GetKey());

            Assert.Equal(HashNotExpired.Count, 2);
            Assert.Equal(HashExpired.Count, 0);

            Assert.NotEqual(JsonNotExpired, "");
            Assert.Equal(JsonExpired, "");
        }

        [Fact]
        public async Task ExpireTestAsync()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            JsonPerson JsonObj = new JsonPerson() { Name = "JsonWithoutExpire" };
            JsonPerson JsonObjWithExpire = new JsonPerson() { Name = "JsonWithExpire" };

            HashPerson HashObj = new HashPerson() { Name = "HashWithoutExpire" };
            HashPerson HashObjWithExpire = new HashPerson() { Name = "HashWithExpire" };

            var JsonObjId = await connection.SetAsync(JsonObj);
            var JsonObjWithExpireId = await connection.SetAsync(JsonObjWithExpire, DateTime.Now.AddMilliseconds(1000));

            var HashObjId = await connection.SetAsync(HashObj);
            var HashObjWithExpireId = await connection.SetAsync(HashObjWithExpire, DateTime.Now.AddMilliseconds(1000));

            Thread.Sleep(1500);

            var HashNotExpired = connection.HGetAll(HashObj.GetKey());
            var HashExpired = connection.HGetAll(HashObjWithExpire.GetKey());

            var JsonNotExpired = connection.JsonGet(JsonObj.GetKey());
            var JsonExpired = connection.JsonGet(JsonObjWithExpire.GetKey());

            Assert.Equal(HashNotExpired.Count, 2);
            Assert.Equal(HashExpired.Count, 0);

            Assert.NotEqual(JsonNotExpired, "");
            Assert.Equal(JsonExpired, "");
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
