using System.Linq;
using NSubstitute;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class GroupByTests
    {
        private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();
        private readonly RedisReply _mockReply = new []
        {
            new RedisReply(1),
            new RedisReply(new RedisReply[]
            {
                "FakeResult",
                "Blah"
            })
        };

        [Fact]
        public void TestSimpleGroupBy()
        {
            var expectedPredicate = "@Name";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.GroupBy(x=>x.RecordShell.Name).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestSimpleGroupBy0()
        {
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "0")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.GroupBy(x=>new{}).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestSimpleGroupByCloseGroup()
        {
            var expectedPredicate = "@Name";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "1",
                    expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.GroupBy(x=>x.RecordShell.Name).CloseGroup().ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGrouby2Fields()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@Age";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "2",
                expectedPredicate1,
                expectedPredicate2)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => x.RecordShell.Name)
                .GroupBy(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGroupby2FieldsAnon()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@Age";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "2",
                    expectedPredicate1,
                    expectedPredicate2)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => new {x.RecordShell.Name, x.RecordShell.Age})
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestGroupby2FieldsAnonAggregation()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@AggAge";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "2",
                    expectedPredicate1,
                    expectedPredicate2)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => new {x.RecordShell.Name, AggAge = x["AggAge"]})
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestGroupby2FieldsOneFromPipeline()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@blah";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "2",
                expectedPredicate1,
                expectedPredicate2)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => x.RecordShell.Name)
                .GroupBy(x => x["blah"])
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

    }
}
