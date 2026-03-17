using System.Collections.Generic;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    public class RedisObjectHandlerTests
    {
        [Fact]
        public void FromHashSet_ReturnsHydratedInstance_ForIRedisHydrateableHashModel()
        {
            var hash = new Dictionary<string, RedisReply>
            {
                { "Name", "Steve" },
            };

            var hydrated = RedisObjectHandler.FromHashSet<HydrateableHashModel>(hash);

            Assert.Equal("Steve", hydrated.Name);
            Assert.Equal("hydrated:Steve", hydrated.HydratedName);
        }

        [Document(StorageType = StorageType.Hash)]
        private class HydrateableHashModel : IRedisHydrateable
        {
            public string Name { get; set; } = string.Empty;

            public string HydratedName { get; private set; } = string.Empty;

            public IDictionary<string, object> BuildHashSet() => new Dictionary<string, object>
            {
                { nameof(Name), Name },
            };

            public void Hydrate(IDictionary<string, string> dict)
            {
                if (dict.TryGetValue(nameof(Name), out var name))
                {
                    HydratedName = $"hydrated:{name}";
                }
            }
        }
    }
}
