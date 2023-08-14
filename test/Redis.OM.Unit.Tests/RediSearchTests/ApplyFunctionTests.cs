using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using System;
using System.Linq;
using NSubstitute;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class ApplyFunctionTests
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
        public void TestStringFormat()
        {
            var expectedPredicate = "format(\"Hello My Name is %s\",@Name)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Apply(x => $"Hello My Name is {x.RecordShell.Name}", "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatPreviousAggregation()
        {
            var expectedPredicate = "format(\"Hello My Name is %s\",@Name)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Apply(x => $"Hello My Name is {(string)x["Name"]}", "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatMultipleReplacements()
        {
            var expectedPredicate = "format(\"Hello My Name is %s and I'm %s\",@Name,@Age)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection.Apply(x => $"Hello My Name is {x.RecordShell.Name} and I'm {x.RecordShell.Age}", "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatMixedUpReplacements()
        {
            var expectedPredicate = "format(\"Hello My Name is %s and I'm %s\",@Name,@Age)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            var res = collection
                .Apply(x => string.Format("Hello My Name is {1} and I'm {0}",
                x.RecordShell.Age,
                x.RecordShell.Name),
                "NamePlease")
                .ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatNonStringLiteral()
        {
            var expectedPredicate = "format(\"Hello My Name is %s\",@Name)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var str = "Hello My Name is {0}";
            var res = collection.Apply(x => string.Format(str, x.RecordShell.Name), "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestToLower()
        {
            var expectedPredicate = "lower(@Name)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.ToLower(), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestToUpper()
        {
            var expectedPredicate = "upper(@Name)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.ToUpper(), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStartWith()
        {
            var expectedPredicate = "startswith(@Name,\"ste\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.StartsWith("ste"), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringContains()
        {
            var expectedPredicate = "contains(@Name,\"ste\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.Contains("ste"), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSubStringSingleArg()
        {
            var expectedPredicate = "substr(@Name,0,-1)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.Substring(0), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSubStringMultiArg()
        {
            var expectedPredicate = "substr(@Name,0,5)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.Substring(0, 5), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSplitNonArray()
        {
            var expectedPredicate = "split(@Name,\"e\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.Split('e', StringSplitOptions.None), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSplitMultiParams()
        {
            var expectedPredicate = "split(@Name,\"e,g\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.Split('e', 'g'), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSplitVarArr()
        {
            var expectedPredicate = "split(@Name,\"e,g\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name")
                .Returns(_mockReply);
            var arr = new[] { 'e', 'g' };
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Name.Split(arr), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestLog()
        {
            var expectedPredicate = "log2(@Age)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Log((int)x.RecordShell.Age), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestLog10()
        {
            var expectedPredicate = "log(@Age)";

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Log10((int)x.RecordShell.Age), "num").ToArray();
            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestCeil()
        {
            var expectedPredicate = "ceil(@Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Ceiling((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFloor()
        {
            var expectedPredicate = "floor(@Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Floor((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestExp()
        {
            var expectedPredicate = "exp(@Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Exp((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestAbs()
        {
            var expectedPredicate = "abs(@Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Abs((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSqrt()
        {
            var expectedPredicate = "sqrt(@Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Sqrt((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMathExpression()
        {
            var expectedPredicate = "@Age - @Height";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => x.RecordShell.Age - x["Height"], "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestOutterMathExpression()
        {
            var expectedPredicate = "abs(@Age) - abs(@Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Abs((int)x.RecordShell.Age) - Math.Abs(x["Height"]), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestInnerMathExpression()
        {
            var expectedPredicate = "abs(@Age - @Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Abs((int)x.RecordShell.Age - x["Height"]), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestInnerInnerMathExpression()
        {
            var expectedPredicate = "abs(sqrt(@Age) - @Height)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(x => Math.Abs(Math.Sqrt((int)x.RecordShell.Age) - x["Height"]), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestInnerInnerMathExpressionLiteral()
        {
            var expectedPredicate = "abs(sqrt(@Age) - 2)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => Math.Abs(Math.Sqrt((int)x.RecordShell.Age) - 2), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDay()
        {
            var expectedPredicate = "day(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Day((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMonth()
        {
            var expectedPredicate = "month(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Month((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMonthOfYear()
        {
            var expectedPredicate = "monthofyear(@LastTimeOnline)";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.MonthOfYear((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestHour()
        {
            var expectedPredicate = "hour(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Hour((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMinute()
        {
            var expectedPredicate = "minute(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Minute((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDayOfWeek()
        {
            var expectedPredicate = "dayofweek(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.DayOfWeek((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDayOfMonth()
        {
            var expectedPredicate = "dayofmonth(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.DayOfMonth((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDayOfYear()
        {
            var expectedPredicate = "dayofyear(@LastTimeOnline)";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.DayOfYear((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestYear()
        {
            var expectedPredicate = "year(@LastTimeOnline)";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Year((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestTimeFormat()
        {
            var expectedPredicate = "timefmt(@LastTimeOnline)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.FormatTimestamp((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestParseTime()
        {
            var expectedPredicate = "parsetime(@TimeString,\"%FT%ZT\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.ParseTime(x.RecordShell.TimeString, "%FT%ZT"), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2Field()
        {
            var expectedPredicate = "geodistance(@Home,@Work)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, (GeoLoc)x.RecordShell.Work), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2String()
        {
            var expectedPredicate = "geodistance(@Home,\"1.5,3.2\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, "1.5,3.2"), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2coords()
        {
            var expectedPredicate = "geodistance(@Home,1.5,3.2)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, 1.5, 3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2coordsTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1Field2coords());
        }

        [Fact]
        public void TestGeoDistance1String2Field()
        {
            var expectedPredicate = "geodistance(\"1.5,3.2\",@Work)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance("1.5,3.2", (GeoLoc)x.RecordShell.Work), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1String2String()
        {
            var expectedPredicate = "geodistance(\"1.5,3.2\",\"1.5,3.2\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance("1.5,3.2", "1.5,3.2"), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1String2Coords()
        {
            var expectedPredicate = "geodistance(\"1.5,3.2\",1.5,3.2)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance("1.5,3.2", 1.5, 3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1String2CoordsTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1String2Coords());
        }

        [Fact]
        public void TestGeoDistance1Coord2Field()
        {
            var expectedPredicate = "geodistance(1.5,3.2,@Work)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, (GeoLoc)x.RecordShell.Work), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2FieldTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1Coord2Field());
        }

        [Fact]
        public void TestGeoDistance1Coord2String()
        {
            var expectedPredicate = "geodistance(1.5,3.2,\"1.5,3.2\")";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, "1.5,3.2"), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2StringTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1Coord2String());
        }

        [Fact]
        public void TestGeoDistance1Coord2Cord()
        {
            var expectedPredicate = "geodistance(1.5,3.2,1.5,3.2)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, 1.5, 3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2CordTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1Coord2Cord());
        }

        [Fact]
        public void TestGeoDistance1Coord2CordAddition()
        {
            var expectedPredicate = "geodistance(1.5,3.2,1.5,3.2) + 2";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, 1.5, 3.2) + 2, "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2CordAdditionTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1Coord2CordAddition());
        }

        [Fact]
        public void TestGeoDistance1Coord2CordNonLiteral()
        {
            var expectedPredicate = "geodistance(1.5,3.2,1.5,3.2)";
            _substitute.Execute(
                "FT.AGGREGATE",
                    Arg.Any<string[]>())
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var lon1 = 1.5;
            var lat1 = 3.2;
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(lon1, lat1, 1.5, 3.2), "geo").ToArray();
            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo");
            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2CordNonLiteralTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => TestGeoDistance1Coord2CordNonLiteral());
        }

        [Fact]
        public void TestExists()
        {
            var expectedPredicate = "exists(@Name)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "exist")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Exists(x.RecordShell.Name), "exist").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestExistsDownPipe()
        {
            var expectedPredicate = "exists(@blah)";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "exist")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.Exists(x["blah"]), "exist").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestBooleanApply()
        {
            var expectedPredicate = "5 > 6";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "boolean")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var five = 5;
            var six = 6;
            var res = collection.Apply(
                x => five > six, "boolean").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestBooleanWithMath()
        {
            var expectedPredicate = "abs(5 + 4) > 6";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "boolean")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var four = 4;
            var five = 5;
            var six = 6;
            var res = collection.Apply(
                x => Math.Abs(five + four) > six, "boolean").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestPow()
        {
            var expectedPredicate = "@Age ^ 4";
            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "boolean")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => x.RecordShell.Age ^ 4, "boolean").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestNestedStringFormat()
        {
            var expectedPredicate = "format(\"Hello My State is %s\",@Address_State)";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    Arg.Any<string[]>())
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            collection.Apply(x => $"Hello My State is {x.RecordShell.Address.State}", "StateText").ToArray();

            _substitute.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "StateText");
        }

        [Fact]
        public void TestNestedNumericApply()
        {
            var expectedPredicate = "@Address_HouseNumber + 4";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    Arg.Any<string[]>())
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);

            collection.Apply(x => x.RecordShell.Address.HouseNumber + 4, "HouseNumPlus4").ToArray();

            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "HouseNumPlus4");
        }

        [Fact]
        public void TestGeoDistanceNested()
        {
            var expectedPredicate = "geodistance(@Home,@Address_Location)";
            _substitute.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "geo")
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, (GeoLoc)x.RecordShell.Address.Location), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestMultipleOperations()
        {
            var expectedPredicate = "@Age + 5 - 6";
            _substitute.Execute(Arg.Any<string>(),
                    Arg.Any<string[]>())
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_substitute);
            var res = collection.Apply(
                x=>x.RecordShell.Age + 5 - 6, "res").ToArray();

            _substitute.Received().Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "res");
        }
    }
}
