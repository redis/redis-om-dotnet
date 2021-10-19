using Moq;
using NRedisPlus.RediSearch;
using System;
using System.Linq;
using NRedisPlus.Contracts;
using NRedisPlus.Model;
using NRedisPlus.RediSearch.AggregationPredicates;
using NRedisPlus.RediSearch.Collections;
using NRedisPlus.Schema;
using Xunit;

namespace NRedisPlus.Unit.Tests.RediSearchTests
{
    public class FilterPredicateTest
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
        public void TestBasicFilter()
        {
            var expectedPredicate = "5 < 6";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var five = 5;
            var six = 6;

            var res = collection.Filter(x=>five<six).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterSingleIdentifier()
        {
            var expectedPredicate = "@Age < 6";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Filter(x => x.RecordShell.Age < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterMathFunctions()
        {
            var expectedPredicate = "abs(@Age) < 6";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Filter(x => Math.Abs((int)x.RecordShell.Age) < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterGeoFunctions()
        {
            var expectedPredicate = "geodistance(@Home,@Work) < 6";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Filter(x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, (GeoLoc)x.RecordShell.Work) < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterStringFunction()
        {
            var expectedPredicate = "contains(@Name,\"ste\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Filter(x => x.RecordShell.Name.Contains("ste")).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterDatetimeFunction()
        {
            var expectedPredicate = "dayofweek(@LastTimeOnline) < 6";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Filter(x => ApplyFunctions.DayOfWeek((long)x.RecordShell.LastTimeOnline) < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
    }
}
