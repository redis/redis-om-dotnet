using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NSubstitute;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class AggregationSetTests
    {
        private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();
        RedisReply MockedResult
        {
            get
            {
                var replyList = new List<RedisReply>();
                replyList.Add(new RedisReply(1));
                for(var i = 0; i < 1000; i++)
                {
                    replyList.Add(new RedisReply(new RedisReply[]
                    {
                        $"FakeResult",
                        "blah"
                    }));
                }
                return new RedisReply[] { replyList.ToArray(), new (5)};
            }
        }

        RedisReply MockedResultCursorEnd
        {
            get
            {
                var replyList = new List<RedisReply>();
                replyList.Add(new RedisReply(1));
                for (var i = 0; i < 1000; i++)
                {
                    replyList.Add(new RedisReply(new RedisReply[]
                    {
                        $"FakeResult",
                        "blah"
                    }));
                }
                return new RedisReply[] { replyList.ToArray(), new RedisReply(0) };
            }
        }

        RedisReply MockedResult10k
        {
            get
            {
                var replyList = new List<RedisReply>();
                replyList.Add(new RedisReply(1));
                for(var i = 0; i < 1000; i++)
                {
                    replyList.Add(new RedisReply(new RedisReply[]
                    {
                        $"FakeResult",
                        "blah"
                    }));
                }
                return new RedisReply[] { replyList.ToArray(), new (5)};
            }
        }

        RedisReply MockedResultCursorEnd10k
        {
            get
            {
                var replyList = new List<RedisReply>();
                replyList.Add(new RedisReply(1));
                for (var i = 0; i < 1000; i++)
                {
                    replyList.Add(new RedisReply(new RedisReply[]
                    {
                        $"FakeResult",
                        "blah"
                    }));
                }
                return new RedisReply[] { replyList.ToArray(), new (0) };
            }
        }


        [Fact]
        public async Task TestAsyncEnumeration()
        {
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "WITHCURSOR",
                "COUNT",
                "1000")
                .Returns(MockedResult);

            _substitute.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",                
                "COUNT",
                "1000")
                .Returns(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            var three = 3;
            var res = collection.Where(x=>x.RecordShell.Age<(int?)three);
            
            var i = 0;
            await foreach (var item in res)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
                i++;
            }

            Assert.Equal(2000, i);
        }

        [Fact]
        public async Task TestToList()
        {
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "WITHCURSOR",
                "COUNT",
                "1000")
                .Returns(MockedResult);

            _substitute.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000")
                .Returns(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            var res = collection.Where(x => x.RecordShell.Age < 3);
            var result = await res.ToListAsync();
            foreach (var item in result)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
            }
            Assert.Equal(2000, result.Count());
            
        }

        [Fact]
        public async Task TestToArray()
        {
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "WITHCURSOR",
                "COUNT",
                "1000")
                .Returns(MockedResult);

            _substitute.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000")
                .Returns(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            var res = collection.Where(x => x.RecordShell.Age < 3);
            var result = await res.ToArrayAsync();
            foreach (var item in result)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
            }
            Assert.Equal(2000, result.Count());
            
        }

        [Fact]
        public async Task TestAsyncEnumerationGrouped()
        {
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "GROUPBY",
                "1",
                "@Height",
                "WITHCURSOR",
                "COUNT",
                "1000")
                .Returns(MockedResult);

            _substitute.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000")
                .Returns(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            var res = collection.Where(x => x.RecordShell.Age < 3).GroupBy(x=>x.RecordShell.Height);

            var i = 0;
            await foreach (var item in res)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
                i++;
            }
            Assert.Equal(2000, i);
        }

        [Fact]
        public async Task TestToListGrouped()
        {
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "GROUPBY",
                "1",
                "@Height",
                "WITHCURSOR",
                "COUNT",
                "1000")
                .Returns(MockedResult);

            _substitute.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000")
                .Returns(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            var res = collection.Where(x => x.RecordShell.Age < 3).GroupBy(x=>x.RecordShell.Height);
            var result = await res.ToListAsync();
            foreach (var item in result)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
            }
            Assert.Equal(2000, result.Count());

        }

        [Fact]
        public async Task TestToArrayGrouped()
        {
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "GROUPBY",
                "1",
                "@Height",
                "WITHCURSOR",
                "COUNT",
                "1000")
                .Returns(MockedResult);

            _substitute.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000")
                .Returns(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            var res = collection.Where(x => x.RecordShell.Age < 3).GroupBy(x=>x.RecordShell.Height);
            var result = await res.ToArrayAsync();
            foreach (var item in result)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
            }
            Assert.Equal(2000, result.Count());

        }

        [Fact]
        public void TestVariableQuery()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true);
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "@Name:fred",
                    "WITHCURSOR",
                    "COUNT",
                    "1000")
                .Returns(MockedResult);

            _substitute.Execute(
                    "FT.CURSOR",
                    "READ",
                    "person-idx",
                    "5",
                    "COUNT",
                    "1000")
                .Returns(MockedResultCursorEnd);

            var fred = "fred";
            var result = collection.Where(x => x.RecordShell.Name == fred).ToList();
            foreach (var item in result)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
            }
        }

        [Fact]
        public void TestConfigurableChunkSize()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize:10000);
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "@Name:fred",
                    "WITHCURSOR",
                    "COUNT",
                    "10000")
                .Returns(MockedResult10k);

            _substitute.Execute(
                    "FT.CURSOR",
                    "READ",
                    "person-idx",
                    "5",
                    "COUNT",
                    "10000")
                .Returns(MockedResultCursorEnd10k);

            var fred = "fred";
            var result = collection.Where(x => x.RecordShell.Name == fred).ToList();
            foreach (var item in result)
            {
                Assert.Equal("blah", item[$"FakeResult"]);
            }
        }

        [Fact]
        public void TestLoad()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.Load(x => x.RecordShell.Name).ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx","*","LOAD","1","Name","WITHCURSOR", "COUNT","10000");
        }
        
        [Fact]
        public void TestMultiVariant()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.Load(x => new {x.RecordShell.Name, x.RecordShell.Age}).ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx","*","LOAD","2","Name", "Age","WITHCURSOR", "COUNT","10000");
        }

        [Fact]
        public void TestLoadAll()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.LoadAll().ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "*", "LOAD", "*", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestMultipleOrderBys()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.OrderBy(x => x.RecordShell.Name).OrderByDescending(x => x.RecordShell.Age).ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx", "*", "SORTBY", "4", "@Name", "ASC", "@Age", "DESC", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestRightSideStringTypeFilter()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);    
            _ = collection.Apply(x => string.Format("{0} {1}", x.RecordShell.FirstName, x.RecordShell.LastName),
                "FullName").Filter(p => p.Aggregations["FullName"] == "Bruce Wayne").ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "*", "APPLY", "format(\"%s %s\",@FirstName,@LastName)", "AS", "FullName", "FILTER", "@FullName == 'Bruce Wayne'", "WITHCURSOR", "COUNT", "10000");
        }
        
        [Fact]
        public void TestNestedOrderBy()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.OrderBy(x => x.RecordShell.Address.State).ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx", "*", "SORTBY", "2", "@Address_State", "ASC", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestNestedGroup()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.GroupBy(x => x.RecordShell.Address.State).ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx", "*", "GROUPBY", "1", "@Address_State", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestNestedGroupMulti()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.GroupBy(x => new {x.RecordShell.Address.State, x.RecordShell.Address.ForwardingAddress.City}).ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx", "*", "GROUPBY", "2", "@Address_State", "@Address_ForwardingAddress_City", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestNestedApply()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.Apply(x => x.RecordShell.Address.HouseNumber + 4, "house_num_modified").ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx", "*", "APPLY", "@Address_HouseNumber + 4", "AS", "house_num_modified", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestMissedBinExpression()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.Apply(x => x.RecordShell.Address.HouseNumber + 4, "house_num_modified")
                .Apply(x=>x.RecordShell.Age + x["house_num_modified"] * 4 + x.RecordShell.Sales, "arbitrary_calculation").ToList();
            _substitute.Received().Execute("FT.AGGREGATE","person-idx", "*", "APPLY", "@Address_HouseNumber + 4", "AS", "house_num_modified", "APPLY", "@Age + @house_num_modified * 4 + @Sales", "AS", "arbitrary_calculation", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestWhereByComplexObjectOnTheRightSide()
        {
            var customerFilter = new CustomerFilterDto()
            {
                FirstName = "James",
                LastName = "Bond"
            };
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.Where(x =>x.RecordShell.FirstName==customerFilter.FirstName) .ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "@FirstName:{James}", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void TestSequentialWhereClauseTranslation()
        {
            var customerFilter = new CustomerFilterDto()
            {
                FirstName = "James",
                LastName = "Bond"
            };
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.Where(x => x.RecordShell.FirstName == customerFilter.FirstName).Where(p=>p.RecordShell.LastName==customerFilter.LastName).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "@LastName:{Bond} @FirstName:{James}", "WITHCURSOR", "COUNT", "10000");
        }
        [Fact]
        public void TestSkipTakeTranslatedLimit()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            _ = collection.OrderByDescending(p=>p.RecordShell.Age).Skip(0).Take(10).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx","*","SORTBY","2","@Age","DESC","LIMIT","0","10", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void RightBinExpressionOperator()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            Expression<Func<AggregationResult<Person>, bool>> query = a => a.RecordShell!.Age == 0 && (a.RecordShell!.Age == 2 || a.RecordShell!.Age == 50);

            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "( @Age:[0 0] ( @Age:[2 2] | @Age:[50 50] ) )", "WITHCURSOR", "COUNT", "10000");
        }
        
        [Fact]
        public void RightBinExpressionWithUniaryOperator()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            Expression<Func<AggregationResult<Person>, bool>> query = a => a.RecordShell!.Name.Contains("Steve") && (a.RecordShell!.Age == 2 || a.RecordShell!.Age == 50);

            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "(@Name:Steve) ( @Age:[2 2] | @Age:[50 50] )", "WITHCURSOR", "COUNT", "10000");
        }
        
        [Fact]
        public void LeftBinExpressionWithUniaryOperator()
        {
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            Expression<Func<AggregationResult<Person>, bool>> query = a => (a.RecordShell!.Age == 2 || a.RecordShell!.Age == 50) && a.RecordShell!.Name.Contains("Steve");

            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "( @Age:[2 2] | @Age:[50 50] ) (@Name:Steve)", "WITHCURSOR", "COUNT", "10000");
        }

        [Fact]
        public void PunctuationMarkInTagQuery()
        {
            var customerFilter = new CustomerFilterDto()
            {
                FirstName = "Walter-Junior",
                LastName = "White"
            };
            var collection = new RedisAggregationSet<Person>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);
            Expression<Func<AggregationResult<Person>, bool>> query = a => a.RecordShell.FirstName == customerFilter.FirstName;
            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "person-idx", "@FirstName:{Walter\\-Junior}", "WITHCURSOR", "COUNT", "10000");
        }
        
        [Fact]
        public void CustomPropertyNamesInQuery()
        {
            //Arrange
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);

            var collection = new RedisAggregationSet<ObjectWithPropertyNamesDefined>(_substitute, true, 10);

            //Act
            _ = collection.Where(x => x.RecordShell.Key == "test").ToList();

            //Assert
            _substitute.Received().Execute("FT.AGGREGATE",
                "objectwithpropertynamesdefined-idx",
                "@notKey:{test}",
                "WITHCURSOR",
                "COUNT",
                "10");
        }
        
        [Fact]
        public void DateTimeQuery()
        {
            var dt = DateTime.Now;
            var dtMs = new DateTimeOffset(dt).ToUnixTimeMilliseconds();

            var dto = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(3));
            var dtoMs = dto.ToUnixTimeMilliseconds();
            var collection = new RedisAggregationSet<ObjectWithDateTime>(_substitute, true, chunkSize: 10000);
            _substitute.Execute("FT.AGGREGATE", Arg.Any<string[]>()).Returns(MockedResult);
            _substitute.Execute("FT.CURSOR", Arg.Any<string[]>()).Returns(MockedResultCursorEnd);

            Expression<Func<AggregationResult<ObjectWithDateTime>, bool>> query = a => a.RecordShell.Timestamp > dt;
            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "objectwithdatetime-idx", $"@Timestamp:[({dtMs} inf]", "WITHCURSOR", "COUNT", "10000");

            query = a => a.RecordShell.Timestamp > dto;
            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "objectwithdatetime-idx", $"@Timestamp:[({dtoMs} inf]", "WITHCURSOR", "COUNT", "10000");

            query = a => a.RecordShell.TimestampOffset > dto;
            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "objectwithdatetime-idx", $"@TimestampOffset:[({dtoMs} inf]", "WITHCURSOR", "COUNT", "10000");

            query = a => a.RecordShell.TimestampOffset > dt;
            _ = collection.Where(query).ToList();
            _substitute.Received().Execute("FT.AGGREGATE", "objectwithdatetime-idx", $"@TimestampOffset:[({dtMs} inf]", "WITHCURSOR", "COUNT", "10000");
        }
    }
}
