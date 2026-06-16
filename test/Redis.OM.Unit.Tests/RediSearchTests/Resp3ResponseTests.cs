using System;
using System.Collections.Generic;
using System.Linq;
using Redis.OM;
using Redis.OM.Aggregation;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    /// <summary>
    /// Covers parsing of RESP3 map-shaped replies for FT.SEARCH and FT.AGGREGATE. Newer
    /// StackExchange.Redis versions negotiate RESP3 automatically, which changes the reply from the
    /// flat RESP2 array into a map keyed by total_results/results - the cause of issue #570
    /// (InvalidCastException "Could not cast to long").
    /// </summary>
    public class Resp3ResponseTests
    {
        private static RedisReply EmptyArray => new(Array.Empty<RedisReply>());

        [Fact]
        public void Search_Resp3Map_ParsesDocuments()
        {
            var reply = SearchMap(new[]
            {
                Result("hash-person-idx:1", score: null, "Id", "1", "Name", "Steve"),
                Result("hash-person-idx:2", score: null, "Id", "2", "Name", "Alice"),
            });

            var response = new SearchResponse<HashPerson>(reply);

            Assert.Equal(2, response.DocumentCount);
            Assert.Equal(2, response.Documents.Count);
            Assert.Equal("Steve", response["hash-person-idx:1"].Name);
            Assert.Equal("Alice", response["hash-person-idx:2"].Name);
            Assert.Empty(response.Scores);
        }

        [Fact]
        public void Search_Resp3Map_WithScores_PopulatesScores()
        {
            var reply = SearchMap(new[]
            {
                Result("hash-person-idx:1", score: "0.5", "Id", "1", "Name", "Steve"),
                Result("hash-person-idx:2", score: "12", "Id", "2", "Name", "Alice"),
            });

            var response = new SearchResponse<HashPerson>(reply);

            Assert.Equal(2, response.Documents.Count);
            Assert.Equal("Steve", response["hash-person-idx:1"].Name);
            Assert.Equal(0.5, response.Scores["hash-person-idx:1"]);
            Assert.Equal(12, response.Scores["hash-person-idx:2"]);
        }

        [Fact]
        public void Search_Resp3Map_NonGeneric_ParsesDocumentsAndScores()
        {
            var reply = SearchMap(new[]
            {
                Result("hash-person-idx:1", score: "38", "Id", "1", "Name", "Steve"),
            });

            var response = new SearchResponse(reply);

            Assert.Single(response.Documents);
            Assert.Equal("Steve", response.Documents["hash-person-idx:1"]["Name"]);
            Assert.Equal(38, response.Scores["hash-person-idx:1"]);
        }

        [Fact]
        public void Search_Resp3Map_EmptyResults_YieldsZeroCount()
        {
            var reply = SearchMap(Array.Empty<RedisReply>());

            var response = new SearchResponse<HashPerson>(reply);

            Assert.Equal(0, response.DocumentCount);
            Assert.Empty(response.Documents);
        }

        [Fact]
        public void Aggregation_Resp3Map_ParsesRows()
        {
            var reply = AggregationMap(new[]
            {
                Map(("type", (RedisReply)"a"), ("cnt", (RedisReply)"3")),
                Map(("type", (RedisReply)"b"), ("cnt", (RedisReply)"5")),
            });

            var results = AggregationResult.FromRedisResult(reply).ToList();

            Assert.Equal(2, results.Count);
            Assert.Equal("a", results[0]["type"].ToString());
            Assert.Equal(3, (int)results[0]["cnt"]);
            Assert.Equal("b", results[1]["type"].ToString());
            Assert.Equal(5, (int)results[1]["cnt"]);
        }

        private static RedisReply Map(params (string Key, RedisReply Value)[] entries)
        {
            var flattened = new List<RedisReply>();
            foreach (var (key, value) in entries)
            {
                flattened.Add(key);
                flattened.Add(value);
            }

            return new RedisReply(flattened.ToArray(), isMap: true);
        }

        private static RedisReply Result(string id, string? score, params string[] fields)
        {
            var entries = new List<(string, RedisReply)> { ("id", id) };
            if (score is not null)
            {
                entries.Add(("score", score));
            }

            var fieldReplies = fields.Select(f => (RedisReply)f).ToArray();
            entries.Add(("extra_attributes", new RedisReply(fieldReplies, isMap: true)));
            entries.Add(("values", EmptyArray));
            return Map(entries.ToArray());
        }

        private static RedisReply SearchMap(RedisReply[] results) => Map(
            ("attributes", EmptyArray),
            ("format", "STRING"),
            ("results", new RedisReply(results)),
            ("total_results", new RedisReply((long)results.Length)),
            ("warning", EmptyArray));

        private static RedisReply AggregationMap(RedisReply[] rows)
        {
            var results = rows.Select(r => Map(("extra_attributes", r), ("values", EmptyArray))).ToArray();
            return Map(
                ("attributes", EmptyArray),
                ("format", "STRING"),
                ("results", new RedisReply(results)),
                ("total_results", new RedisReply((long)rows.Length)),
                ("warning", EmptyArray));
        }
    }
}
