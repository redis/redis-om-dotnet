using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class ReducerTests
    {

        private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();

        [Fact]
        public void TestSumNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_SUM",
                    5.0
                })
            };

            var expectedPredicate = "@Age";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "SUM",
                "1",
                expectedPredicate,
                "AS",
                "Age_SUM").Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Sum(x => x.RecordShell.Age);

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestSumAsync()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_SUM",
                    5.0
                })
            };

            var expectedPredicate = "@Age";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "SUM",
                "1",
                expectedPredicate,
                "AS",
                "Age_SUM")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Sum(x => x.RecordShell.Age);

            Assert.Equal(5, res);
        }
        
        [Fact]
        public void TestMultiple()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_SUM",
                    5.0
                })
            };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<string[]>()).Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            _ = collection
                .GroupBy(x=>x.RecordShell.TagField)
                .Sum(x => x.RecordShell.Sales)
                .Average(x=>x.RecordShell.Age)
                .ToList();

            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@TagField",
                "REDUCE",
                "SUM",
                "1",
                "@Sales",
                "AS",
                "Sales_SUM",
                "REDUCE",
                "AVG",
                "1",
                "@Age",
                "AS",
                "Age_AVG");
        
        }
        
        [Fact]
        public void TestThree()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_SUM",
                    5.0
                })
            };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<string[]>()).Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            _ = collection
                .GroupBy(x=>x.RecordShell.TagField)
                .Sum(x => x.RecordShell.Sales)
                .Average(x=>x.RecordShell.Age)
                .Sum(x=>x.RecordShell.Age)
                .ToList();

            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@TagField",
                "REDUCE",
                "SUM",
                "1",
                "@Sales",
                "AS",
                "Sales_SUM",
                "REDUCE",
                "AVG",
                "1",
                "@Age",
                "AS",
                "Age_AVG",
                "REDUCE",
                "SUM",
                "1",
                "@Age",
                "AS",
                "Age_SUM");
        
        }

        [Fact]
        public void TestAverageNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_AVG",
                    5
                })
            };

            var expectedPredicate = "@Age";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "AVG",
                "1",
                expectedPredicate,
                "AS",
                "Age_AVG")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Average(x => x.RecordShell.Age);

            Assert.Equal(5, res);
        }

        [Fact]
        public async Task TestAverageNoGroupPredicateAsync()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_AVG",
                    5
                })
            };

            var expectedPredicate = "@Age";
            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "AVG",
                "1",
                expectedPredicate,
                "AS",
                "Age_AVG")
                .Returns(mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = await collection.AverageAsync(x => x.RecordShell.Age);

            Assert.Equal(5, res);
            
        }

        [Fact]
        public void TestAverageWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_AVG",
                    5
                })
            };

            var expectedPredicate = "@Age";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "AVG",
                "1",
                expectedPredicate,
                "AS",
                "Age_AVG")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.GroupBy(x=>x.RecordShell.Height).Average(x => x.RecordShell.Age).ToArray();

            Assert.Equal(5, (int)res[0]["Age_AVG"]);
        }

        [Fact]
        public void TestCountNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "COUNT",
                    5
                })
            };
            
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "COUNT",
                "0",
                "AS",
                "COUNT")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Count();

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestCountWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "COUNT",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "COUNT",
                "0",
                "AS",
                "COUNT")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.GroupBy(x=>x.RecordShell.Height).Count();

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestLongCountNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "COUNT",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "COUNT",
                "0",
                "AS",
                "COUNT")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.LongCount();

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestLongCountWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "COUNT",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "COUNT",
                "0",
                "AS",
                "COUNT")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.GroupBy(x=>x.RecordShell.Height).LongCount();

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestCountDistinctNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_COUNT_DISTINCT",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "COUNT_DISTINCT",
                "1",
                "@Age",
                "AS",
                "Age_COUNT_DISTINCT")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.CountDistinct(x=>x.RecordShell.Age);

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestCountDistinctWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_COUNT_DISTINCT",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "COUNT_DISTINCT",
                "1",
                "@Age",
                "AS",
                "Age_COUNT_DISTINCT")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => x.RecordShell.Height)
                .CountDistinct(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal(5, (int)res[0]["Age_COUNT_DISTINCT"]);
        }

        [Fact]
        public void TestCountDistinctIshNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_COUNT_DISTINCTISH",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",                
                "REDUCE",
                "COUNT_DISTINCTISH",
                "1",
                "@Age",
                "AS",
                "Age_COUNT_DISTINCTISH")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .CountDistinctish(x => x.RecordShell.Age);

            Assert.Equal(5, res);
        }

        [Fact]
        public async Task TestCountDistinctIshNoGroupPredicateAsync()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_COUNT_DISTINCTISH",
                    5
                })
            };

            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "COUNT_DISTINCTISH",
                "1",
                "@Age",
                "AS",
                "Age_COUNT_DISTINCTISH")
                .Returns(mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = await collection
                .CountDistinctishAsync(x => x.RecordShell.Age);

            Assert.Equal(5, res);
            
        }

        [Fact]
        public void TestCountDistinctIshWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_COUNT_DISTINCTISH",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "COUNT_DISTINCTISH",
                "1",
                "@Age",
                "AS",
                "Age_COUNT_DISTINCTISH")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => x.RecordShell.Height)
                .CountDistinctish(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal(5, (int)res[0]["Age_COUNT_DISTINCTISH"]);
        }

        [Fact]
        public void TestStandardDeviationNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_STDDEV",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "STDDEV",
                "1",
                "@Age",
                "AS",
                "Age_STDDEV")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.StandardDeviation(x => x.RecordShell.Age);

            Assert.Equal(5, res);
        }

        [Fact]
        public async Task TestStandardDeviationNoGroupPredicateAsync()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_STDDEV",
                    5
                })
            };

            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "STDDEV",
                "1",
                "@Age",
                "AS",
                "Age_STDDEV")
                .Returns(mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = await collection.StandardDeviationAsync(x => x.RecordShell.Age);

            Assert.Equal(5, res);            
        }

        [Fact]
        public void TestStandardDeviationWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_STDDEV",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "STDDEV",
                "1",
                "@Age",
                "AS",
                "Age_STDDEV")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x=>x.RecordShell.Height)
                .StandardDeviation(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal(5, (int)res[0]["Age_STDDEV"]);
        }

        [Fact]
        public void TestQuantileNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_QUANTILE_0.7",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "QUANTILE",
                "2",
                "@Age",
                "0.7",
                "AS",
                "Age_QUANTILE_0.7")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Quantile(x => x.RecordShell.Age, .7);

            Assert.Equal(5, res);
        }

        [Fact]
        public void TestQuantileNoGroupPredicateTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", _ => TestQuantileNoGroupPredicate());
        }

        [Fact]
        public async Task TestQuantileNoGroupPredicateAsync()

        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_QUANTILE_0.7",
                    5
                })
            };

            _substitute.ExecuteAsync(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "GROUPBY",
                    "0",
                    "REDUCE",
                    "QUANTILE",
                    "2",
                    "@Age",
                    "0.7",
                    "AS",
                    "Age_QUANTILE_0.7")
                .Returns(mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = await collection.QuantileAsync(x => x.RecordShell.Age, .7);
            Assert.Equal(5, res);
        }

        [Fact]
        public void TestQuantileNoGroupPredicateAsyncTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", _ => TestQuantileNoGroupPredicateAsync());
        }

        [Fact]
        public void TestQuantileWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_QUANTILE_0.7",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "QUANTILE",
                "2",
                "@Age",
                "0.7",
                "AS",
                "Age_QUANTILE_0.7")
                .Returns((RedisReply)mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x=>x.RecordShell.Height)
                .Quantile(x => x.RecordShell.Age, .7)
                .ToArray();

            Assert.Equal(5, (int)res[0]["Age_QUANTILE_0.7"]);
        }

        [Fact]
        public void TestQuantileWithGroupPredicateTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", _ => TestQuantileWithGroupPredicate());
        }

        [Fact]
        public void TestDistinctNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_TOLIST",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "TOLIST",
                "1",
                "@Age",
                "AS",
                "Age_TOLIST")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Distinct(x => x.RecordShell.Age).ToArray();
            Assert.Equal(5, (int)res[0]["Age_TOLIST"]);
        }        

        [Fact]
        public void TestDistinctWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_TOLIST",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "TOLIST",
                "1",
                "@Age",
                "AS",
                "Age_TOLIST")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x=>x.RecordShell.Height)
                .Distinct(x => x.RecordShell.Age).ToArray();


            Assert.Equal(5, (int)res[0]["Age_TOLIST"]);
        }

        [Fact]
        public void TestFirstValueNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_FIRST_VALUE",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "FIRST_VALUE",
                "1",
                "@Age",
                "AS",
                "Age_FIRST_VALUE")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.FirstValue(x => x.RecordShell.Age);
            Assert.Equal(5, (int)res);
        }

        [Fact]
        public async Task TestFirstValueNoGroupPredicateAsync()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_FIRST_VALUE",
                    5
                })
            };

            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "FIRST_VALUE",
                "1",
                "@Age",
                "AS",
                "Age_FIRST_VALUE")
                .Returns(mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = await collection.FirstValueAsync(x => x.RecordShell.Age);
            Assert.Equal(5, (int)res);
        }

        [Fact]
        public void TestFirstValueWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_FIRST_VALUE",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "FIRST_VALUE",
                "1",
                "@Age",
                "AS",
                "Age_FIRST_VALUE")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x=>x.RecordShell.Height)
                .FirstValue(x => x.RecordShell.Age)
                .ToArray();
            Assert.Equal(5, (int)res[0]["Age_FIRST_VALUE"]);
        }

        [Fact]
        public void TestFirstValueSortPropertyNoDirectionNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_FIRST_VALUE",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "FIRST_VALUE",
                "3",
                "@Age",
                "BY",
                "@Height",
                "AS",
                "Age_FIRST_VALUE")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.FirstValue(x => x.RecordShell.Age, nameof(Person.Height));
            Assert.Equal(5, (int)res);
        }

        [Fact]
        public void TestFirstValueSortPropertyNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_FIRST_VALUE",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "FIRST_VALUE",
                "4",
                "@Age",
                "BY",
                "@Height",
                "ASC",
                "AS",
                "Age_FIRST_VALUE")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.FirstValue(x => x.RecordShell.Age, nameof(Person.Height), SortDirection.Ascending);
            Assert.Equal(5, (int)res);
        }

        [Fact]
        public void TestRandomSampleNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_RANDOM_SAMPLE_20",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "RANDOM_SAMPLE",
                "2",
                "@Age",
                "20",
                "AS",
                "Age_RANDOM_SAMPLE_20")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.RandomSample(x => x.RecordShell.Age, 20).ToArray();
            Assert.Equal(5, (long)res[0]["Age_RANDOM_SAMPLE_20"]);
        }

        [Fact]
        public void TestRandomSampleWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_RANDOM_SAMPLE_20",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "RANDOM_SAMPLE",
                "2",
                "@Age",
                "20",
                "AS",
                "Age_RANDOM_SAMPLE_20")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x=>x.RecordShell.Height)
                .RandomSample(x => x.RecordShell.Age, 20)
                .ToArray();

            Assert.Equal(5, (long)res[0]["Age_RANDOM_SAMPLE_20"]);
        }

        [Fact]
        public void TestMinNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_MIN",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "MIN",
                "1",
                "@Age",
                "AS",
                "Age_MIN")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Min(x => x.RecordShell.Age);
            Assert.Equal(5, res);
        }

        [Fact]
        public void TestMinWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_MIN",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "MIN",
                "1",
                "@Age",
                "AS",
                "Age_MIN")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x=>x.RecordShell.Height)
                .Min(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal(5, (int)res[0]["Age_MIN"]);
        }

        [Fact]
        public void TestMaxNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_MAX",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "MAX",
                "1",
                "@Age",
                "AS",
                "Age_MAX")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Max(x => x.RecordShell.Age);
            Assert.Equal(5, res);
        }

        [Fact]
        public async Task TestMaxAsyncNoGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_MAX",
                    5
                })
            };

            _substitute.ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "0",
                "REDUCE",
                "MAX",
                "1",
                "@Age",
                "AS",
                "Age_MAX")
                .Returns(mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = await collection.MaxAsync(x => x.RecordShell.Age);
            Assert.Equal(5, (int)res);
            
        }

        [Fact]
        public void TestMaxWithGroupPredicate()
        {
            var mockReply = new []
            {
                new RedisReply(1),
                new RedisReply(new RedisReply[]
                {
                    "Age_MAX",
                    5
                })
            };

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "MAX",
                "1",
                "@Age",
                "AS",
                "Age_MAX")
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .GroupBy(x => x.RecordShell.Height)
                .Max(x => x.RecordShell.Age)
                .ToArray();

            Assert.Equal(5, (int)res[0]["Age_MAX"]);
        }

        [Fact]
        public void TestCountMembers()
        {
            var mockReply = new []
            {
                new RedisReply(1),
            };
            
            _substitute.Execute(
                    Arg.Any<string>(),
                    Arg.Any<string[]>())
                .Returns((RedisReply)mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            _ = collection.GroupBy(x => x.RecordShell.Height).CountGroupMembers().ToList();
            
            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "GROUPBY",
                "1",
                "@Height",
                "REDUCE",
                "COUNT",
                "0",
                "AS",
                "COUNT");
        }
    }
}
