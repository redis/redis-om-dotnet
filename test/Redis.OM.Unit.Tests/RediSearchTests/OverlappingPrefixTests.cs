using System;
using System.Linq;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Collection("Redis")]
    public class OverlappingPrefixTests
    {
        [Document(StorageType = StorageType.Json, IndexName = "prefix-player-idx", Prefixes = new[] { "Player" })]
        private class PlayerPrefixDocument
        {
            [RedisIdField]
            [Indexed]
            public string Id { get; set; }

            [Searchable]
            public string Name { get; set; }
        }

        [Document(StorageType = StorageType.Json, IndexName = "prefix-player-state-idx", Prefixes = new[] { "PlayerState" })]
        private class PlayerStatePrefixDocument
        {
            [RedisIdField]
            [Indexed]
            public string Id { get; set; }

            [Searchable]
            public string Status { get; set; }
        }

        private readonly IRedisConnection _connection;

        public OverlappingPrefixTests(RedisSetup setup)
        {
            _connection = setup.Connection;
        }

        [Fact]
        public async Task CollectionsWithOverlappingPrefixes_DoNotLeakResults()
        {
            CleanupArtifacts();

            try
            {
                await _connection.CreateIndexAsync(typeof(PlayerPrefixDocument));
                await _connection.CreateIndexAsync(typeof(PlayerStatePrefixDocument));

                var playerCollection = new RedisCollection<PlayerPrefixDocument>(_connection);
                var playerStateCollection = new RedisCollection<PlayerStatePrefixDocument>(_connection);

                await playerCollection.InsertAsync(new PlayerPrefixDocument { Name = "alice" });
                await playerStateCollection.InsertAsync(new PlayerStatePrefixDocument { Status = "online" });

                var players = await playerCollection.ToListAsync();
                var playerStates = await playerStateCollection.ToListAsync();

                Assert.Single(players);
                Assert.Equal("alice", players[0].Name);
                Assert.Single(playerStates);
                Assert.Equal("online", playerStates[0].Status);
            }
            finally
            {
                CleanupArtifacts();
            }
        }

        private void CleanupArtifacts()
        {
            TryDropIndexAndAssociatedRecords(typeof(PlayerPrefixDocument));
            TryDropIndexAndAssociatedRecords(typeof(PlayerStatePrefixDocument));
            DeleteKeysByPattern("Player:*");
            DeleteKeysByPattern("PlayerState:*");
        }

        private void DeleteKeysByPattern(string pattern)
        {
            var keys = _connection.Execute("KEYS", pattern).ToArray().Select(x => x.ToString()).ToArray();
            if (keys.Length > 0)
            {
                _connection.Execute("DEL", keys.Cast<object>().ToArray());
            }
        }

        private void TryDropIndexAndAssociatedRecords(Type type)
        {
            try
            {
                _connection.DropIndexAndAssociatedRecords(type);
            }
            catch (Exception ex) when (ex.Message.Contains("Unknown Index name") || ex.Message.Contains("no such index"))
            {
            }
        }
    }
}
