using Moq;
using Redis.OM.RediSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Model;
using Redis.OM.RediSearch.Collections;
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


        [Fact]
        public void TestAsyncEnumeration()
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
            Task.Run(async () =>
            {
                await foreach (var item in res)
                {
                    Assert.Equal("blah", item[$"FakeResult"]);
                    i++;
                }
                Assert.Equal(2000, i);
            }).GetAwaiter().GetResult();
            
        }

        [Fact]
        public void TestToList()
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
            Task.Run(async () =>
            {
                var result = await res.ToListAsync();
                foreach (var item in result)
                {
                    Assert.Equal("blah", item[$"FakeResult"]);
                }
                Assert.Equal(2000, result.Count());
            }).GetAwaiter().GetResult();
            
        }

        [Fact]
        public void TestToArray()
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
            Task.Run(async () => 
            {
                var result = await res.ToArrayAsync();
                foreach (var item in result)
                {
                    Assert.Equal("blah", item[$"FakeResult"]);
                }
                Assert.Equal(2000, result.Count());
            }).GetAwaiter().GetResult();
            
        }

        [Fact]
        public void TestAsyncEnumerationGrouped()
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
            Task.Run(async () =>
            {
                await foreach (var item in res)
                {
                    Assert.Equal("blah", item[$"FakeResult"]);
                    i++;
                }
                Assert.Equal(2000, i);
            }).GetAwaiter().GetResult();

        }

        [Fact]
        public void TestToListGrouped()
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
            Task.Run(async () =>
            {
                var result = await res.ToListAsync();
                foreach (var item in result)
                {
                    Assert.Equal("blah", item[$"FakeResult"]);
                }
                Assert.Equal(2000, result.Count());
            }).GetAwaiter().GetResult();

        }

        [Fact]
        public void TestToArrayGrouped()
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
            Task.Run(async () =>
            {
                var result = await res.ToArrayAsync();
                foreach (var item in result)
                {
                    Assert.Equal("blah", item[$"FakeResult"]);
                }
                Assert.Equal(2000, result.Count());
            }).GetAwaiter().GetResult();

        }
    }
}
