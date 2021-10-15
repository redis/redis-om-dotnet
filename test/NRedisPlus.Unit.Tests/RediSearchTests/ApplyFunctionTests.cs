using Moq;
using NRedisPlus.RediSearch;
using System;
using System.Linq;
using NRedisPlus.Contracts;
using Xunit;

namespace NRedisPlus.Unit.Tests.RediSearchTests
{
    public class ApplyFunctionTests
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
        public void TestStringFormat()
        {
            var expectedPredicate = "format(\"Hello My Name is %s\",@Name)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Apply(x => string.Format("Hello My Name is {0}", x.RecordShell.Name), "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatPreviousAggregation()
        {
            var expectedPredicate = "format(\"Hello My Name is %s\",@Name)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Apply(x => string.Format("Hello My Name is {0}", (string)x["Name"]), "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatMultipleReplacements()
        {
            var expectedPredicate = "format(\"Hello My Name is %s and I'm %s\",@Name,@Age)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

            var res = collection.Apply(x => string.Format("Hello My Name is {0} and I'm {1}", x.RecordShell.Name, x.RecordShell.Age), "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringFormatMixedUpRepalcements()
        {
            var expectedPredicate = "format(\"Hello My Name is %s and I'm %s\",@Name,@Age)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);

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
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "NamePlease"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var str = "Hello My Name is {0}";
            var res = collection.Apply(x => string.Format(str, x.RecordShell.Name), "NamePlease").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestTooLower()
        {
            var expectedPredicate = "lower(@Name)";            
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.ToLower(),"Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestTooUpper()
        {
            var expectedPredicate = "upper(@Name)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.ToUpper(), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStartWith()
        {
            var expectedPredicate = "startswith(@Name,\"ste\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.StartsWith("ste"), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestStringContains()
        {
            var expectedPredicate = "contains(@Name,\"ste\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.Contains("ste"), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSubStringSingleArg()
        {
            var expectedPredicate = "substr(@Name,0,-1)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.Substring(0), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSubStringMultiArg()
        {
            var expectedPredicate = "substr(@Name,0,5)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.Substring(0,5), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSplitNonArray()
        {
            var expectedPredicate = "split(@Name,\"e\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.Split('e', StringSplitOptions.None), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSplitMultiParams()
        {
            var expectedPredicate = "split(@Name,\"e,g\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);

            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.Split('e','g'), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSplitVarArr()
        {
            var expectedPredicate = "split(@Name,\"e,g\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "Name"))
                .Returns(_mockReply);
            var arr = new[] { 'e', 'g' };
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Name.Split(arr), "Name").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestLog()
        {
            var expectedPredicate = "log2(@Age)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Log((int)x.RecordShell.Age), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestLog10()
        {
            var expectedPredicate = "log(@Age)";
            
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            
            var collection = new RedisAggregationSet<Person>(_mock.Object);                        
            var res = collection.Apply(x => Math.Log10((int)x.RecordShell.Age), "num").ToArray();            
            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestCeil()
        {
            var expectedPredicate = "ceil(@Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Ceiling((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestFloor()
        {
            var expectedPredicate = "floor(@Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Floor((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestExp()
        {
            var expectedPredicate = "exp(@Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Exp((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestAbs()
        {
            var expectedPredicate = "abs(@Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Abs((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestSqrt()
        {
            var expectedPredicate = "sqrt(@Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Sqrt((double)x.RecordShell.Height), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMathExpression()
        {
            var expectedPredicate = "@Age - @Height";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => x.RecordShell.Age - x["Height"], "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestOutterMathExpression()
        {
            var expectedPredicate = "abs(@Age) - abs(@Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Abs((int)x.RecordShell.Age) - Math.Abs(x["Height"]), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestInnerMathExpression()
        {
            var expectedPredicate = "abs(@Age - @Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Abs((int)x.RecordShell.Age - x["Height"]), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestInnerInnerMathExpression()
        {
            var expectedPredicate = "abs(sqrt(@Age) - @Height)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(x => Math.Abs(Math.Sqrt((int)x.RecordShell.Age) - x["Height"]), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestInnerInnerMathExpressionLiteral()
        {
            var expectedPredicate = "abs(sqrt(@Age) - 2)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "num"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => Math.Abs(Math.Sqrt((int)x.RecordShell.Age) - 2), "num").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDay()
        {
            var expectedPredicate = "day(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.Day((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMonth()
        {
            var expectedPredicate = "month(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.Month((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestMonthOfYear()
        {
            var expectedPredicate = "monthofyear(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.MonthOfYear((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestHour()
        {
            var expectedPredicate = "hour(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);            
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.Hour((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestMinute()
        {
            var expectedPredicate = "minute(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.Minute((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDayOfWeek()
        {
            var expectedPredicate = "dayofweek(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.DayOfWeek((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestDayOfMonth()
        {
            var expectedPredicate = "dayofmonth(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.DayOfMonth((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestDayOfYear()
        {
            var expectedPredicate = "dayofyear(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.DayOfYear((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
        
        [Fact]
        public void TestYear()
        {
            var expectedPredicate = "year(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                    "FT.AGGREGATE",
                    "person-idx",
                    "*",
                    "APPLY",
                    expectedPredicate,
                    "AS",
                    "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.Year((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestTimeFormat()
        {
            var expectedPredicate = "timefmt(@LastTimeOnline)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.FormatTimestamp((long)x.RecordShell.LastTimeOnline), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestParseTime()
        {
            var expectedPredicate = "parsetime(@TimeString,\"%FT%ZT\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "time"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.ParseTime(x.RecordShell.TimeString, "%FT%ZT"), "time").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2Field()
        {
            var expectedPredicate = "geodistance(@Home,@Work)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, (GeoLoc)x.RecordShell.Work), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2String()
        {
            var expectedPredicate = "geodistance(@Home,\"1.5,3.2\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, "1.5,3.2"), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Field2coords()
        {
            var expectedPredicate = "geodistance(@Home,1.5,3.2)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance((GeoLoc)x.RecordShell.Home, 1.5,3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1String2Field()
        {
            var expectedPredicate = "geodistance(\"1.5,3.2\",@Work)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance("1.5,3.2", (GeoLoc)x.RecordShell.Work), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }        

        [Fact]
        public void TestGeoDistance1String2String()
        {
            var expectedPredicate = "geodistance(\"1.5,3.2\",\"1.5,3.2\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance("1.5,3.2", "1.5,3.2"), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1String2Coords()
        {
            var expectedPredicate = "geodistance(\"1.5,3.2\",1.5,3.2)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance("1.5,3.2", 1.5,3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2Field()
        {
            var expectedPredicate = "geodistance(1.5,3.2,@Work)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5,3.2, (GeoLoc)x.RecordShell.Work), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2String()
        {
            var expectedPredicate = "geodistance(1.5,3.2,\"1.5,3.2\")";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, "1.5,3.2"), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2Cord()
        {
            var expectedPredicate = "geodistance(1.5,3.2,1.5,3.2)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, 1.5,3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2CordAddition()
        {
            var expectedPredicate = "geodistance(1.5,3.2,1.5,3.2) + 2";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(1.5, 3.2, 1.5, 3.2) + 2, "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestGeoDistance1Coord2CordNonLiteral()
        {
            var expectedPredicate = "geodistance(1.5,3.2,1.5,3.2)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "geo"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var lon1 = 1.5;
            var lat1 = 3.2;
            var res = collection.Apply(
                x => ApplyFunctions.GeoDistance(lon1, lat1, 1.5, 3.2), "geo").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestExists()
        {
            var expectedPredicate = "exists(@Name)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "exist"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => ApplyFunctions.Exists(x.RecordShell.Name), "exist").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestExistsDownPipe()
        {
            var expectedPredicate = "exists(@blah)";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "exist"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);       
            var res = collection.Apply(
                x => ApplyFunctions.Exists(x["blah"]), "exist").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }

        [Fact]
        public void TestBooleanApply()
        {
            var expectedPredicate = "5 > 6";
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "boolean"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
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
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "boolean"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
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
            _mock.Setup(x => x.Execute(
                "FT.AGGREGATE",
                "person-idx",
                "*",
                "APPLY",
                expectedPredicate,
                "AS",
                "boolean"))
                .Returns(_mockReply);
            var collection = new RedisAggregationSet<Person>(_mock.Object);
            var res = collection.Apply(
                x => x.RecordShell.Age ^ 4, "boolean").ToArray();

            Assert.Equal("Blah", res[0]["FakeResult"]);
        }
    }
}
