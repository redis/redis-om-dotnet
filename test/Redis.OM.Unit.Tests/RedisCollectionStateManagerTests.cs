using System.Collections.Generic;
using System.Linq;
using Redis.OM.Modeling;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    public class RedisCollectionStateManagerTests
    {
        [Document(StorageType = StorageType.Json)]
        public class DictionaryModel
        {
            public string? Name { get; set; }

            public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        }

        [Theory]
        [InlineData("plain", "$.Tags.plain")]
        [InlineData("with space", "$.Tags[\"with space\"]")]
        [InlineData("tab\tkey", "$.Tags[\"tab\tkey\"]")]
        [InlineData("a.b", "$.Tags[\"a.b\"]")]
        [InlineData("quo\"te", "$.Tags[\"quo\\\"te\"]")]
        [InlineData("back\\slash", "$.Tags[\"back\\\\slash\"]")]
        public void DictionaryKeysProduceValidJsonPaths(string key, string expectedPath)
        {
            var manager = new RedisCollectionStateManager(typeof(DictionaryModel).GetCustomAttributes(typeof(DocumentAttribute), false).Cast<DocumentAttribute>().First());
            var model = new DictionaryModel { Name = "x", Tags = new Dictionary<string, string> { ["seed"] = "v0" } };
            manager.InsertIntoSnapshot("k", model);

            model.Tags[key] = "v1";

            Assert.True(manager.TryDetectDifferencesSingle("k", model, out var diffs));
            var args = Assert.Single(diffs!).SerializeScriptArgs();
            Assert.Equal("SET", args[0]);
            Assert.Equal(expectedPath, args[1]);
            Assert.Equal("\"v1\"", args[2]);
        }
    }
}
