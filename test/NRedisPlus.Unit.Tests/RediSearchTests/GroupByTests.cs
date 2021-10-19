using Moq;
using NRedisPlus.RediSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRedisPlus.Contracts;
using NRedisPlus.Model;
using NRedisPlus.RediSearch.Collections;
using Xunit;

namespace NRedisPlus.Unit.Tests.RediSearchTests
{
    public class GroupByTests
    {
        Mock<IRedisConnection> _mock = new Mock<IRedisConnection>();
        RedisReply _mockReply = new RedisReply[]
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
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.GroupBy(x=>x.RecordShell.Name).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestSimpleGroupBy0()
        {
            var expectedPredicate = "@Name";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "0"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.GroupBy(x=>new{}).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestSimpleGroupByCloseGroup()
        {
            var expectedPredicate = "@Name";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "1",
                    expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.GroupBy(x=>x.RecordShell.Name).CloseGroup().ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGrouby2Fields()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@Age";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "2",
                expectedPredicate1,
                expectedPredicate2))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection
                .GroupBy(x => x.RecordShell.Name)
                .GroupBy(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGrouby2FieldsAnon()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@Age";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "2",
                    expectedPredicate1,
                    expectedPredicate2))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection
                .GroupBy(x => new {x.RecordShell.Name, x.RecordShell.Age})
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestGrouby2FieldsAnonAggregation()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@AggAge";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "2",
                    expectedPredicate1,
                    expectedPredicate2))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection
                .GroupBy(x => new {x.RecordShell.Name, AggAge = x["AggAge"]})
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestGrouby2FieldsOneFromPipeline()
        {
            var expectedPredicate1 = "@Name";
            var expectedPredicate2 = "@blah";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "2",
                expectedPredicate1,
                expectedPredicate2))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection
                .GroupBy(x => x.RecordShell.Name)
                .GroupBy(x => x["blah"])
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

    }
}
