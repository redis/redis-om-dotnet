using Xunit;
using System;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Linq;
using System.IO;
using Redis.OM.Modeling;
using System.Threading;
using System.Threading.Tasks;
using Redis.OM.Searching;
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

            var hashNotExpired = connection.HGetAll(hashObj.GetKey());
            var hashExpired = connection.HGetAll(hashObjWithExpire.GetKey());

            var jsonNotExpired = connection.JsonGet(jsonObj.GetKey());
            var jsonExpired = connection.JsonGet(jsonObjWithExpire.GetKey());

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

            var hashNotExpired = await connection.HGetAllAsync(hashObj.GetKey());
            var hashExpired = await connection.HGetAllAsync(hashObjWithExpire.GetKey());

            var jsonNotExpired = await connection.JsonGetAsync(jsonObj.GetKey());
            var jsonExpired = await connection.JsonGetAsync(jsonObjWithExpire.GetKey());

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
        public void SimpleJsonSetWhen()
        {
            var keyName = "test-json:SimpleJsonSetWhen";
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            connection.Unlink(keyName);
            var obj = new ModelExampleJson { Name = "Shachar", Age = 23 };

            Assert.False(connection.JsonSet(keyName, ".", obj, WhenKey.Exists));
            Assert.True(connection.JsonSet(keyName, ".", obj, WhenKey.NotExists));
            var reconsitutedObject = connection.JsonGet<ModelExampleJson>(keyName);

            Assert.Equal("Shachar", reconsitutedObject.Name);
            Assert.Equal(23, reconsitutedObject.Age);

            obj.Name = "Shachar2";
            Assert.False(connection.JsonSet(keyName, ".", obj, WhenKey.NotExists));
            reconsitutedObject = connection.JsonGet<ModelExampleJson>(keyName);
            Assert.Equal("Shachar", reconsitutedObject.Name);

            Assert.True(connection.JsonSet(keyName, ".", obj, WhenKey.Exists));
            reconsitutedObject = connection.JsonGet<ModelExampleJson>(keyName);

            Assert.Equal("Shachar2", reconsitutedObject.Name);
            connection.Unlink(keyName);
        }

        [Fact]
        public async Task SimpleJsonSetWhenAsync()
        {
            var keyName = "test-json:SimpleJsonSetWhenAsync";
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;

            connection.Unlink(keyName);
            var obj = new ModelExampleJson { Name = "Shachar", Age = 23 };
            
            Assert.False(await connection.JsonSetAsync(keyName, ".", obj, WhenKey.Exists));
            Assert.True(await connection.JsonSetAsync(keyName, ".", obj, WhenKey.NotExists));
            var reconsitutedObject = connection.JsonGet<ModelExampleJson>(keyName);

            Assert.Equal("Shachar", reconsitutedObject.Name);
            Assert.Equal(23, reconsitutedObject.Age);

            obj.Name = "Shachar2";
            Assert.False(await connection.JsonSetAsync(keyName, ".", obj, WhenKey.NotExists));
            reconsitutedObject = connection.JsonGet<ModelExampleJson>(keyName);
            Assert.Equal("Shachar", reconsitutedObject.Name);

            Assert.True(await connection.JsonSetAsync(keyName, ".", obj, WhenKey.Exists));
            reconsitutedObject = connection.JsonGet<ModelExampleJson>(keyName);

            Assert.Equal("Shachar2", reconsitutedObject.Name);
            connection.Unlink(keyName);
        }
        
        [Fact]
        public async Task SimpleHashInsertWhenAsync()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            var collection = new RedisCollection<HashPerson>(provider.Connection);

            var obj = new HashPerson() { Name = "Steve", Age = 33, Email = "foo@bar.com"};
            var key = await collection.InsertAsync(obj, WhenKey.NotExists);
            Assert.NotNull(key);
            var reconstituted = await collection.FindByIdAsync(key);
            Assert.NotNull(reconstituted);
            Assert.Equal("Steve", reconstituted.Name);
            Assert.Equal(33, reconstituted.Age);
            obj.Name = "Shachar";
            obj.Age = null;

            var res = await collection.InsertAsync(obj, WhenKey.NotExists); // this should fail 
            Assert.Null(res);
            res = await collection.InsertAsync(obj, WhenKey.Exists); // this should work
            Assert.NotNull(res);
            Assert.Equal(key,res);
            reconstituted = await collection.FindByIdAsync(key);
            Assert.NotNull(reconstituted);
            Assert.Null(reconstituted.Age);
            Assert.Equal("Shachar" , reconstituted.Name);

            await connection.UnlinkAsync(key);
            await collection.InsertAsync(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            await Task.Delay(1000);
            res = await collection.InsertAsync(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.Null(res);
            var expiration = (long)await connection.ExecuteAsync("PTTL", key);
            Assert.True(expiration < 4050, $"Expiration is {expiration}");
            res = await collection.InsertAsync(obj, WhenKey.Exists, TimeSpan.FromMilliseconds(5000));
            Assert.NotNull(res);

            await connection.UnlinkAsync(key);
            res = await collection.InsertAsync(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.NotNull(res);
            await connection.UnlinkAsync(key);
        }
        
        [Fact]
        public void SimpleHashInsertWhen()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            var collection = new RedisCollection<HashPerson>(provider.Connection);

            var obj = new HashPerson() { Name = "Steve", Age = 33, Email = "foo@bar.com"};
            var key = collection.Insert(obj, WhenKey.NotExists);
            Assert.NotNull(key);
            var reconstituted = collection.FindById(key);
            Assert.NotNull(reconstituted);
            Assert.Equal("Steve", reconstituted.Name);
            Assert.Equal(33, reconstituted.Age);
            obj.Name = "Shachar";
            obj.Age = null;

            var res = collection.Insert(obj, WhenKey.NotExists); // this should fail 
            Assert.Null(res);
            res = collection.Insert(obj, WhenKey.Exists); // this should work
            Assert.NotNull(res);
            Assert.Equal(key,res);
            reconstituted = collection.FindById(key);
            Assert.NotNull(reconstituted);
            Assert.Null(reconstituted.Age);
            Assert.Equal("Shachar" , reconstituted.Name);

            connection.Unlink(key);
            collection.Insert(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Thread.Sleep(1100);
            res = collection.Insert(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.Null(res);
            var expiration = (long)connection.Execute("PTTL", key);
            Assert.True(expiration <= 4050);
            res = collection.Insert(obj, WhenKey.Exists, TimeSpan.FromMilliseconds(5000));
            Assert.NotNull(res);

            connection.Unlink(key);
            res = collection.Insert(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.NotNull(res);
            connection.Unlink(key);
        }
        
        [Fact]
        public async Task SimpleJsonInsertWhenAsync()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            var collection = new RedisCollection<BasicJsonObject>(provider.Connection);

            var obj = new BasicJsonObject { Name = "Steve" };
            var key = await collection.InsertAsync(obj, WhenKey.NotExists);
            Assert.NotNull(key);
            var reconstituted = await collection.FindByIdAsync(key);
            Assert.NotNull(reconstituted);
            Assert.Equal("Steve", reconstituted.Name);
            obj.Name = "Shachar";

            var res = await collection.InsertAsync(obj, WhenKey.NotExists); // this should fail 
            Assert.Null(res);
            res = await collection.InsertAsync(obj, WhenKey.Exists); // this should work
            Assert.NotNull(res);
            Assert.Equal(key,res);
            reconstituted = await collection.FindByIdAsync(key);
            Assert.NotNull(reconstituted);
            Assert.Equal("Shachar" , reconstituted.Name);

            await connection.UnlinkAsync(key);
            var k2 = await collection.InsertAsync(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.NotNull(k2);
            Assert.Equal(key, k2);
            Assert.Equal(key.Split(":")[1], obj.Id);
            await Task.Delay(1000);
            Assert.True(connection.Execute("EXISTS", key) == 1, $"Expected: {key} to exist, it did not.");
            res = await collection.InsertAsync(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.Null(res);
            var expiration = (long)await connection.ExecuteAsync("PTTL", key);
            Assert.True(expiration <= 4050, $"Actual: {expiration}");
            res = await collection.InsertAsync(obj, WhenKey.Exists, TimeSpan.FromMilliseconds(5000));
            expiration = (long)await connection.ExecuteAsync("PTTL", key);
            Assert.NotNull(res);
            Assert.True(expiration >= 4000, $"Actual: {expiration}");
            res = collection.Insert(obj, WhenKey.Always, TimeSpan.FromMilliseconds(6000));
            expiration = (long)connection.Execute("PTTL", key);
            Assert.NotNull(res);
            Assert.True(expiration>=5000);
            res = collection.Insert(obj, WhenKey.Always);
            expiration = (long)connection.Execute("PTTL", key);
            Assert.NotNull(res);
            Assert.True(-1 == expiration, $"expiry was: {expiration}");
            connection.Unlink(key);
            await connection.UnlinkAsync(key);
        }
        
        [Fact]
        public void SimpleJsonInsertWhen()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
            var provider = new RedisConnectionProvider($"redis://{host}");
            var connection = provider.Connection;
            var collection = new RedisCollection<BasicJsonObject>(provider.Connection);

            var obj = new BasicJsonObject { Name = "Steve" };
            var key = collection.Insert(obj, WhenKey.NotExists);
            Assert.NotNull(key);
            var reconstituted = collection.FindById(key);
            Assert.NotNull(reconstituted);
            Assert.Equal("Steve", reconstituted.Name);
            obj.Name = "Shachar";

            var res = collection.Insert(obj, WhenKey.NotExists); // this should fail 
            Assert.Null(res);
            res = collection.Insert(obj, WhenKey.Exists); // this should work
            Assert.NotNull(res);
            Assert.Equal(key,res);
            reconstituted = collection.FindById(key);
            Assert.NotNull(reconstituted);
            Assert.Equal("Shachar" , reconstituted.Name);

            connection.Unlink(key);
            collection.Insert(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Thread.Sleep(1100);
            res = collection.Insert(obj, WhenKey.NotExists, TimeSpan.FromMilliseconds(5000));
            Assert.Null(res);
            var expiration = (long)connection.Execute("PTTL", key);
            Assert.True(expiration <= 4050, $"Expiration: {expiration}");
            res = collection.Insert(obj, WhenKey.Exists, TimeSpan.FromMilliseconds(5000));
            Assert.NotNull(res);
            res = collection.Insert(obj, WhenKey.Always, TimeSpan.FromMilliseconds(6000));
            Assert.NotNull(res);
            res = collection.Insert(obj, WhenKey.Always);
            expiration = (long)connection.Execute("PTTL", key);
            Assert.NotNull(res);
            Assert.True(-1 == expiration, $"expiry was: {expiration}");
            connection.Unlink(key);
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

        [SkipIfMissingEnvVar("AGGRESSIVELY_SHORT_TIMEOUT_REDIS")]
        public async Task SearchTimeoutTest()
        {
            var hostInfo = Environment.GetEnvironmentVariable("AGGRESSIVELY_SHORT_TIMEOUT_REDIS") ?? string.Empty;
            Console.WriteLine($"Current host info: {hostInfo}");
            if (string.IsNullOrEmpty(hostInfo))
            {
                throw new ArgumentException(
                    "AGGRESSIVELY_SHORT_TIMEOUT_REDIS environment variable is not set - please set an instance with a 1 ms timeout for searches with on timeout set to fail");
            }

            var provider = new RedisConnectionProvider($"redis://{hostInfo}");
            provider.Connection.CreateIndex(typeof(Person));
            var collection = provider.RedisCollection<Person>();

            var tasks = new List<Task>();
            for (var i = 0; i < 10000; i++)
            {
                tasks.Add(collection.InsertAsync(new Person() { Name = "Timeout Person", Age = 35 }));
            }

            await Task.WhenAll(tasks);

            var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await collection.Take(10000).ToListAsync());
            Assert.Equal("Encountered timeout when searching - check the duration of your query.", ex.Message);
        }
    }
}