using System;
using System.Linq;
using NSubstitute;
using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class FilterPredicateTest
    {
        private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();
        RedisReply _mockReply = new []
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
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var five = 5;
            var six = 6;

            var res = collection.Filter(x=>five<six).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestBasicFilterString()
        {
            var expectedPredicate = "@Name == 'steve'";
            _substitute.Execute(
                    "FT.AGGREGATE",Arg.Any<string[]>()
                )
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Filter(x=>x.RecordShell.Name == "steve").ToArray();

            _substitute.Received().Execute("FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate);
            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestBasicFilterStringUnpackedFromVariable()
        {
            var expectedPredicate = "@Name == 'steve'";
            _substitute.Execute(
                    "FT.AGGREGATE",Arg.Any<string[]>()
                )
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var steve = "steve";
            var res = collection.Filter(x=>x.RecordShell.Name == steve).ToArray();

            _substitute.Received().Execute("FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate);
            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestBasicFilterNullableString()
        {
            var expectedPredicate = "@NullableStringField == 'steve'";
            _substitute.Execute(
                    "FT.AGGREGATE",Arg.Any<string[]>()
                )
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            
            var res = collection.Filter(x=>x.RecordShell.NullableStringField == "steve").ToArray();

            _substitute.Received().Execute("FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate);
            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterSingleIdentifier()
        {
            var expectedPredicate = "@Age < 6";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Filter(x => x.RecordShell.Age < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterMathFunctions()
        {
            var expectedPredicate = "abs(@Age) < 6";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Filter(x => Math.Abs((int)x.RecordShell.Age) < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterGeoFunctions()
        {
            var expectedPredicate = "geodistance(@Home,@Work) < 6";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Filter(x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, (GeoLoc)x.RecordShell.Work) < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterStringFunction()
        {
            var expectedPredicate = "contains(@Name,\"ste\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Filter(x => x.RecordShell.Name.Contains("ste")).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFilterDatetimeFunction()
        {
            var expectedPredicate = "dayofweek(@LastTimeOnline) < 6";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "FILTER",
                expectedPredicate)
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Filter(x => ApplyFunctions.DayOfWeek((long)x.RecordShell.LastTimeOnline) < 6).ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
    }
}
