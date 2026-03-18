using Redis.OM;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class SearchResponseTests
    {
        [Fact]
        public void WithScoresResponse_AllowsDuplicateScores_ParsesDocumentIdsCorrectly()
        {
            var response = new SearchResponse<HashPerson>(new RedisReply[]
            {
                new(2),
                new("hash-person-idx:1"),
                new(38),
                new RedisReply[]
                {
                    "Id",
                    "1",
                    "Name",
                    "Steve",
                },
                new("hash-person-idx:2"),
                new(38),
                new RedisReply[]
                {
                    "Id",
                    "2",
                    "Name",
                    "Alice",
                },
            });

            Assert.Equal(2, response.DocumentCount);
            Assert.Equal(2, response.Documents.Count);
            Assert.Equal("1", response["hash-person-idx:1"].Id);
            Assert.Equal("Steve", response["hash-person-idx:1"].Name);
            Assert.Equal("2", response["hash-person-idx:2"].Id);
            Assert.Equal("Alice", response["hash-person-idx:2"].Name);
        }

        [Fact]
        public void WithScoresResponse_HandlesDistinctScores_ParsesDocumentIdsCorrectly()
        {
            var response = new SearchResponse<HashPerson>(new RedisReply[]
            {
                new(2),
                new("hash-person-idx:1"),
                new(12),
                new RedisReply[]
                {
                    "Id",
                    "1",
                    "Name",
                    "Steve",
                },
                new("hash-person-idx:2"),
                new(87),
                new RedisReply[]
                {
                    "Id",
                    "2",
                    "Name",
                    "Alice",
                },
            });

            Assert.Equal(2, response.DocumentCount);
            Assert.Equal(2, response.Documents.Count);
            Assert.Equal("1", response["hash-person-idx:1"].Id);
            Assert.Equal("Steve", response["hash-person-idx:1"].Name);
            Assert.Equal("2", response["hash-person-idx:2"].Id);
            Assert.Equal("Alice", response["hash-person-idx:2"].Name);
        }
    }
}
