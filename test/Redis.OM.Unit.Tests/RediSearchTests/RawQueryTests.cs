using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class RawQueryTests
    {
        private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();
        private readonly RedisReply _mockReply = new RedisReply[]
        {
            new(1),
            new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            })
        };

        private readonly RedisReply _mockReplyMultiple = new RedisReply[]
        {
            new(2),
            new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            }),
            new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY74O"),
            new(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Alice\",\"Age\":28,\"Height\":67.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY74O\"}"
            })
        };

        private readonly RedisReply _mockAggregationReply = new RedisReply[]
        {
            new(1),
            new RedisReply[]
            {
                new("Age"),
                new("32")
            }
        };

        private readonly RedisReply _mockAggregationMultipleReply = new RedisReply[]
        {
            new(2),
            new RedisReply[]
            {
                new("DepartmentNumber"),
                new("1"),
                new("avg_age"),
                new("35")
            },
            new RedisReply[]
            {
                new("DepartmentNumber"),
                new("2"),
                new("avg_age"),
                new("55")
            }
        };

        [Fact]
        public void TestRawQuery_Basic()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("@Name:{Steve}").ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "@Name:{Steve}",
                "LIMIT",
                "0",
                "100");

            Assert.Single(result);
            Assert.Equal("Steve", result[0].Name);
            Assert.Equal(32, result[0].Age);
            Assert.Equal(71.0, result[0].Height);
        }

        [Fact]
        public void TestRawQuery_ComplexCondition()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("@Age:[30 40] @Height:[70 +inf]").ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "@Age:[30 40] @Height:[70 +inf]",
                "LIMIT",
                "0",
                "100");

            Assert.Single(result);
        }

        [Fact]
        public void TestRawQuery_WithLIMITParameters()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyMultiple);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("*").Skip(1).Take(1).ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "1",
                "1");
        }

        [Fact]
        public void TestRawQuery_WithORDERBYParameters()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyMultiple);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("*").OrderBy(p => p.Age).ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "SORTBY",
                "Age",
                "ASC");
        }

        [Fact]
        public void TestRawQuery_WithORDERBYDescendingParameters()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyMultiple);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("*").OrderByDescending(p => p.Age).ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "SORTBY",
                "Age",
                "DESC");
        }

        [Fact]
        public void TestRawQuery_WithWhereClauseChaining()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("@Age:[30 40]").Where(p => p.Name == "Steve").ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[30 40] (@Name:\"Steve\"))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestRawQuery_FirstOrDefault()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var result = collection.Raw("@Name:{Steve}").FirstOrDefault();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "@Name:{Steve}",
                "LIMIT",
                "0",
                "1");

            Assert.Equal("Steve", result.Name);
        }

        [Fact]
        public void TestRawAggregation_BasicQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockAggregationReply);

            var aggSet = new RedisAggregationSet<Person>(_substitute);
            // Raw should only set the query part (*)
            var result = aggSet.Raw("*").GroupBy(x => x.RecordShell.Age).ToList();

            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Age");

            Assert.Single(result);
            Assert.Equal("32", result[0]["Age"].ToString());
        }

        [Fact]
        public void TestRawAggregation_WithCustomFilter()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockAggregationMultipleReply);

            var aggSet = new RedisAggregationSet<Person>(_substitute);
            // Raw should only set the query part (@Age:[30 +inf])
            var result = aggSet.Raw("@Age:[30 +inf]")
                .GroupBy(x => x.RecordShell.DepartmentNumber)
                .Average(x => x.RecordShell.Age)
                .ToList();

            _substitute.Received().Execute(
                Arg.Is<string>(s => s == "FT.AGGREGATE"),
                Arg.Is<string>(s => s == "person-idx"),
                Arg.Is<string>(s => s == "@Age:[30 +inf]"),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<object>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<object>());

            Assert.Equal(2, result.Count);
            
            var dept1 = result.FirstOrDefault(r => r["DepartmentNumber"].ToString() == "1");
            var dept2 = result.FirstOrDefault(r => r["DepartmentNumber"].ToString() == "2");
            
            Assert.NotNull(dept1);
            Assert.NotNull(dept2);
            Assert.Equal("35", dept1["avg_age"].ToString());
            Assert.Equal("55", dept2["avg_age"].ToString());
        }

        [Fact]
        public void TestRawAggregation_WithPipelineOperations()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockAggregationReply);

            var aggSet = new RedisAggregationSet<Person>(_substitute);
            // Raw only sets the filter query part, skip/take still work as expected
            var result = aggSet.Raw("@Age > 30").Skip(0).Take(10).ToList();

            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "@Age > 30",
                "LIMIT",
                "0",
                "10");
        }
        
        [Fact]
        public void TestRawAggregation_ComplexFilter()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockAggregationReply);

            var aggSet = new RedisAggregationSet<Person>(_substitute);
            
            // Use Raw to set a complex filter
            var result = aggSet.Raw("(@Age:[30 40] | @Name:Steve) @Height:[70 +inf]")
                .GroupBy(x => x.RecordShell.DepartmentNumber)
                .CountGroupMembers()
                .ToList();

            _substitute.Received().Execute(
                Arg.Is<string>(s => s == "FT.AGGREGATE"),
                Arg.Is<string>(s => s == "person-idx"),
                Arg.Is<string>(s => s == "(@Age:[30 40] | @Name:Steve) @Height:[70 +inf]"),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<object>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<object>());
        }
    }
}