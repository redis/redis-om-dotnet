using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Redis.OM;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class AggregationSetTests
    {
        Mock<IRedisConnection> _mock = new Mock<IRedisConnection>();
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
                return new RedisReply[] { replyList.ToArray(), new RedisReply(5)};
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
                return new RedisReply[] { replyList.ToArray(), new RedisReply(5)};
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
                return new RedisReply[] { replyList.ToArray(), new RedisReply(0) };
            }
        }


        [Fact]
        public async Task TestAsyncEnumeration()
        {
            _mock.Setup(x => x.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "WITHCURSOR",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResult);

            _mock.Setup(x => x.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",                
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
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
            _mock.Setup(x => x.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "WITHCURSOR",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResult);

            _mock.Setup(x => x.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
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
            _mock.Setup(x => x.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "WITHCURSOR",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResult);

            _mock.Setup(x => x.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
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
            _mock.Setup(x => x.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "GROUPBY",
                "1",
                "@Height",
                "WITHCURSOR",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResult);

            _mock.Setup(x => x.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
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
            _mock.Setup(x => x.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "GROUPBY",
                "1",
                "@Height",
                "WITHCURSOR",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResult);

            _mock.Setup(x => x.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
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
            _mock.Setup(x => x.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Age:[-inf (3]",
                "GROUPBY",
                "1",
                "@Height",
                "WITHCURSOR",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResult);

            _mock.Setup(x => x.ExecuteAsync(
                "FT.CURSOR",
                "READ",
                "person-idx",
                "5",
                "COUNT",
                "1000"))
                .ReturnsAsync(MockedResultCursorEnd);
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
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
            var collection = new RedisAggregationSet<Person>(_mock.Object, true);
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "@Name:fred",
                    "WITHCURSOR",
                    "COUNT",
                    "1000"))
                .Returns(MockedResult);

            _mock.Setup(x => x.Execute(
                    "FT.CURSOR",
                    "READ",
                    "person-idx",
                    "5",
                    "COUNT",
                    "1000"))
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
            var collection = new RedisAggregationSet<Person>(_mock.Object, true, chunkSize:10000);
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "@Name:fred",
                    "WITHCURSOR",
                    "COUNT",
                    "10000"))
                .Returns(MockedResult10k);

            _mock.Setup(x => x.Execute(
                    "FT.CURSOR",
                    "READ",
                    "person-idx",
                    "5",
                    "COUNT",
                    "10000"))
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
            var collection = new RedisAggregationSet<Person>(_mock.Object, true, chunkSize: 10000);
            _mock.Setup(x => x.Execute("FT.AGGREGATE", It.IsAny<string[]>())).Returns(MockedResult);
            _mock.Setup(x => x.Execute("FT.CURSOR", It.IsAny<string[]>())).Returns(MockedResultCursorEnd);
            collection.Load(x => x.RecordShell.Name).ToList();
            _mock.Verify(x=>x.Execute("FT.AGGREGATE","person-idx","*","LOAD","1","Name","WITHCURSOR", "COUNT","10000"));
        }
        
        [Fact]
        public void TestMultiVariant()
        {
            var collection = new RedisAggregationSet<Person>(_mock.Object, true, chunkSize: 10000);
            _mock.Setup(x => x.Execute("FT.AGGREGATE", It.IsAny<string[]>())).Returns(MockedResult);
            _mock.Setup(x => x.Execute("FT.CURSOR", It.IsAny<string[]>())).Returns(MockedResultCursorEnd);
            collection.Load(x => new {x.RecordShell.Name, x.RecordShell.Age}).ToList();
            _mock.Verify(x=>x.Execute("FT.AGGREGATE","person-idx","*","LOAD","2","Name", "Age","WITHCURSOR", "COUNT","10000"));
        }

        [Fact]
        public void TestLoadAll()
        {
            var collection = new RedisAggregationSet<Person>(_mock.Object, true, chunkSize: 10000);
            _mock.Setup(x => x.Execute("FT.AGGREGATE", It.IsAny<string[]>())).Returns(MockedResult);
            _mock.Setup(x => x.Execute("FT.CURSOR", It.IsAny<string[]>())).Returns(MockedResultCursorEnd);
            collection.LoadAll().ToList();
            _mock.Verify(x =>
                x.Execute("FT.AGGREGATE", "person-idx", "*", "LOAD", "*", "WITHCURSOR", "COUNT", "10000"));
        }

        [Fact]
        public void TestMultipleOrderBys()
        {
            var collection = new RedisAggregationSet<Person>(_mock.Object, true, chunkSize: 10000);
            _mock.Setup(x => x.Execute("FT.AGGREGATE", It.IsAny<string[]>())).Returns(MockedResult);
            _mock.Setup(x => x.Execute("FT.CURSOR", It.IsAny<string[]>())).Returns(MockedResultCursorEnd);
            _ = collection.OrderBy(x => x.RecordShell.Name).OrderByDescending(x => x.RecordShell.Age).ToList();
            _mock.Verify(x=>x.Execute("FT.AGGREGATE","person-idx", "*", "SORTBY", "4", "@Name", "ASC", "@Age", "DESC", "WITHCURSOR", "COUNT", "10000"));
        }

        [Fact]
        public void TestRightSideStringTypeFilter()
        {
            var collection = new RedisAggregationSet<Person>(_mock.Object, true, chunkSize: 10000);
            _mock.Setup(x => x.Execute("FT.AGGREGATE", It.IsAny<string[]>())).Returns(MockedResult);
            _mock.Setup(x => x.Execute("FT.CURSOR", It.IsAny<string[]>())).Returns(MockedResultCursorEnd);
            _ = collection.Apply(x => string.Format("{0} {1}", x.RecordShell.FirstName, x.RecordShell.LastName),
                "FullName").Filter(p => p.Aggregations["FullName"] == "Bruce Wayne").ToList();
            _mock.Verify(x => x.Execute("FT.AGGREGATE", "person-idx", "*", "APPLY", "format(\"%s %s\",@FirstName,@LastName)", "AS", "FullName", "FILTER", "@FullName == 'Bruce Wayne'", "WITHCURSOR", "COUNT", "10000"));
        }
    }
}
