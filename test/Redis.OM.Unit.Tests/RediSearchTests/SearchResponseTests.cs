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

            Assert.Equal(2, response.Scores.Count);
            Assert.Equal(38, response.Scores["hash-person-idx:1"]);
            Assert.Equal(38, response.Scores["hash-person-idx:2"]);
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

            Assert.Equal(2, response.Scores.Count);
            Assert.Equal(12, response.Scores["hash-person-idx:1"]);
            Assert.Equal(87, response.Scores["hash-person-idx:2"]);
        }

        [Fact]
        public void WithScoresResponse_ParsesFractionalScores()
        {
            // DIALECT 2 / TFIDF-style scoring can return fractional scores as bulk strings.
            var response = new SearchResponse<HashPerson>(new RedisReply[]
            {
                new(1),
                new("hash-person-idx:1"),
                new("0.5"),
                new RedisReply[]
                {
                    "Id",
                    "1",
                    "Name",
                    "Steve",
                },
            });

            Assert.Single(response.Documents);
            Assert.Equal(0.5, response.Scores["hash-person-idx:1"]);
        }

        [Fact]
        public void WithoutScores_ScoresDictionaryIsEmpty()
        {
            var response = new SearchResponse<HashPerson>(new RedisReply[]
            {
                new(1),
                new("hash-person-idx:1"),
                new RedisReply[]
                {
                    "Id",
                    "1",
                    "Name",
                    "Steve",
                },
            });

            Assert.Single(response.Documents);
            Assert.Empty(response.Scores);
        }

        [Fact]
        public void WithScoresResponse_NonGeneric_CapturesScores()
        {
            var response = new SearchResponse(new RedisReply[]
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
                new(87),
                new RedisReply[]
                {
                    "Id",
                    "2",
                    "Name",
                    "Alice",
                },
            });

            Assert.Equal(2, response.Documents.Count);
            Assert.Equal("Steve", response.Documents["hash-person-idx:1"]["Name"]);
            Assert.Equal(38, response.Scores["hash-person-idx:1"]);
            Assert.Equal(87, response.Scores["hash-person-idx:2"]);
        }
    }
}
