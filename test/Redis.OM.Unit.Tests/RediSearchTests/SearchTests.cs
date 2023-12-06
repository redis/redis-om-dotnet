#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Redis.OM.Common;
using Xunit;
using Redis.OM.Searching.Query;
using StackExchange.Redis;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class SearchTests
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

        private readonly RedisReply _mockReplyNone = new RedisReply[]
        {
            new (0),
        };

        private readonly RedisReply _mockReply2Count = new []
        {
            new RedisReply(2),
            new RedisReply("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new RedisReply(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            })
        };

        [Fact]
        public void TestBasicQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33).ToList();
            _substitute.Received().Execute("FT.SEARCH",
                "person-idx",
                "(@Age:[-inf (33])",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicNegationQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => !(x.Age < 33)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "-(@Age:[-inf (33])",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryWithVariable()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < y).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[-inf (33])",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestFirstOrDefaultWithMixedLocals()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var heightList = new List<double> { 70.0, 68.0 };
            var y = 33;
            foreach (var height in heightList)
            {
                var collection = new RedisCollection<Person>(_substitute);
                _ = collection.FirstOrDefault(x => x.Age == y && x.Height == height);
                _substitute.Received().Execute(
                    "FT.SEARCH",
                    "person-idx",
                    $"((@Age:[33 33]) (@Height:[{height} {height}]))",
                    "LIMIT",
                    "0",
                    "1");
            }
        }

        [Fact]
        public void TestBasicQueryWithExactIntegerMatch()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age == y).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryWithExactDecimalMatch()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var y = 90.5M;
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Salary == y).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Salary:[90.5 90.5])",
                "LIMIT",
                "0",
                "100");
        }

        [Theory]
        [InlineData("en-DE")]
        [InlineData("it-IT")]
        [InlineData("es-ES")]
        public void TestBasicQueryWithExactDecimalMatchTestingInvariantCultureCompliance(string lcid)
        {
            Helper.RunTestUnderDifferentCulture(lcid, _ => TestBasicQueryWithExactDecimalMatch());
        }

        [Fact]
        public void TestBasicFirstOrDefaultQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.FirstOrDefault(x => x.Age == y);
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public void TestBasicQueryNoNameIndex()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<PersonNoName>(_substitute);
            _ = collection.FirstOrDefault(x => x.Age == y);
            _substitute.Received().Execute(
                "FT.SEARCH",
                "personnoname-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public void TestBasicOrQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33 || x.TagField == "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) | (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicOrQueryTwoTags()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.TagField == "Bob" || x.TagField == "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{Bob}) | (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicOrQueryWithNegation()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33 || x.TagField != "Steve" || x.Name != "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(((@Age:[-inf (33]) | (-@TagField:{Steve})) | (-@Name:\"Steve\"))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicAndQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33 && x.TagField == "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicTagQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33 && x.TagField == "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicThreeClauseQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33 && x.TagField == "Steve" && x.Height >= 70).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(((@Age:[-inf (33]) (@TagField:{Steve})) (@Height:[70 inf]))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestGroupedThreeClauseQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age < 33 && (x.TagField == "Steve" || x.Height >= 70)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) ((@TagField:{Steve}) | (@Height:[70 inf])))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryWithContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.Contains("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryWithStartsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.StartsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste*)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestFuzzy()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.FuzzyMatch("Ste", 2)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:%%Ste%%)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestMatchStartsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.MatchStartsWith("Ste")).ToList();
            _substitute.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste*)",
                "LIMIT",
                "0",
                "100");
        }
        
        [Fact]
        public void TestMatchEndsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.MatchEndsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestMatchContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.MatchContains("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste*)",
                "LIMIT",
                "0",
                "100");
        }
        
        [Fact]
        public void TestTagContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var ste = "Ste";
            var person = new Person() { TagField = "ath" };
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.TagField.Contains(ste)).ToList();
            _ = collection.Where(x => x.TagField.Contains(person.TagField)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{*Ste*})",
                "LIMIT",
                "0",
                "100");

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{*ath*})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestTagStartsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.TagField.StartsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{Ste*})",
                "LIMIT",
                "0",
                "100");
        }
        
        [Fact]
        public void TestTagEndsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.TagField.EndsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{*Ste})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestTextEndsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.EndsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste)",
                "LIMIT",
                "0",
                "100");
        }
        
        [Fact]
        public void TestTextStartsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.StartsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste*)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryWithEndsWith()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.EndsWith("Ste")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryFromPropertyOfModel()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var modelObject = new Person() { Name = "Steve" };
            _ = collection.Where(x => x.Name == modelObject.Name).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"Steve\")",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryFromPropertyOfModelWithStringInterpolation()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var modelObject = new Person() { Name = "Steve" };
            _ = collection.Where(x => x.Name == $"A {nameof(Person)} named {modelObject.Name}").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"A Person named Steve\")",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryFromPropertyOfModelWithStringFormatFourArgs()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var modelObject = new Person() { Name = "Steve" };
            var a = "A";
            var named = "named";
            _ = collection.Where(x => x.Name == $"{a} {nameof(Person)} {named} {modelObject.Name}").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"A Person named Steve\")",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestBasicQueryWithContainsWithNegation()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => !x.Name.Contains("Ste")).ToList();
            _substitute.Execute(
                "FT.SEARCH",
                "person-idx",
                "-(@Name:Ste)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestTwoPredicateQueryWithContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.Contains("Ste") || x.TagField == "John").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Name:Ste) | (@TagField:{John}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestTwoPredicateQueryWithPrefixMatching()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name.Contains("Ste*") || x.TagField == "John").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Name:Ste*) | (@TagField:{John}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestGeoFilter()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "GEOFILTER",
                "Home",
                "5",
                "6.7",
                "50",
                "km"
            );
        }

        [Fact]
        public void TestGeoFilterWithWhereClause()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            var res = collection.Where(x => x.TagField == "Steve").GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            Assert.Equal(32, res[0].Age);
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{Steve})",
                "LIMIT",
                "0",
                "100",
                "GEOFILTER",
                "Home",
                "5",
                "6.7",
                "50",
                "km"
            );
        }

        [Fact]
        public void TestSelect()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Select(x => x.Name).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "Name"
            );
        }

        [Fact]
        public void TestSelectComplexAnonType()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Select(x => new { x.Name }).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "3",
                "Name",
                "AS",
                "Name");
        }

        [Fact]
        public void TextEqualityExpression()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Name == "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"Steve\")",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestPaginationChunkSizesSinglePredicate()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.Name == "Steve").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"Steve\")",
                "LIMIT",
                "0",
                "1000");
        }

        [Fact]
        public void TestPaginationChunkSizesMultiplePredicates()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.TagField == "Steve").GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{Steve})",
                "LIMIT",
                "0",
                "1000",
                "GEOFILTER",
                "Home",
                "5",
                "6.7",
                "50",
                "km"
            );
        }

        [Fact]
        public void TestNestedObjectStringSearch()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.Address.City == "Newark").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_City:{Newark})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestNestedObjectStringSearchNested2Levels()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.Address.ForwardingAddress.City == "Newark").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_ForwardingAddress_City:{Newark})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestNestedObjectNumericSearch()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.Address.HouseNumber == 4).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_HouseNumber:[4 4])",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestNestedObjectNumericSearch2Levels()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.Address.ForwardingAddress.HouseNumber == 4).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_ForwardingAddress_HouseNumber:[4 4])",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestNestedQueryOfGeo()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.GeoFilter(x => x.Address.Location, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "1000",
                "GEOFILTER",
                "Address_Location",
                "5",
                "6.7",
                "50",
                "km"
            );
        }

        [Fact]
        public void TestNestedQueryOfGeo2Levels()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.GeoFilter(x => x.Address.ForwardingAddress.Location, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "1000",
                "GEOFILTER",
                "Address_ForwardingAddress_Location",
                "5",
                "6.7",
                "50",
                "km"
            );
        }

        [Fact]
        public void TestArrayContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.NickNames.Contains("Steve")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestArrayContainsSpecialChar()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.NickNames.Contains("Steve@redis.com")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve\\@redis\\.com})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestArrayContainsVar()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            var steve = "Steve";
            _ = collection.Where(x => x.NickNames.Contains(steve)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void TestArrayContainsNested()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.Mother.NickNames.Contains("Di")).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Mother_NickNames:{Di})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public async Task TestUpdateJson()
        {
            _substitute.ExecuteAsync("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);
            
            _substitute.ExecuteAsync("EVALSHA", Arg.Any<object[]>()).Returns(Task.FromResult(new RedisReply("42")));
            _substitute.ExecuteAsync("SCRIPT", Arg.Any<object[]>())
                .Returns(Task.FromResult(new RedisReply("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb")));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = await collection.FirstAsync(x => x.Name == "Steve");
            steve.Age = 33;
            await collection.UpdateAsync(steve);
            await _substitute.Received().ExecuteAsync("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33");
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonUnloadedScriptAsync()
        {

            _substitute.ExecuteAsync("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);

            _substitute.ExecuteAsync("EVALSHA", Arg.Any<object[]>())
                .Throws(new RedisServerException("Failed on EVALSHA"));
            _substitute.ExecuteAsync("EVAL", Arg.Any<object[]>()).Returns(Task.FromResult(new RedisReply("42")));
            _substitute.ExecuteAsync("SCRIPT", Arg.Any<object[]>()).Returns(Task.FromResult(new RedisReply("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb")));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = await collection.FirstAsync(x => x.Name == "Steve");
            steve.Age = 33;
            await collection.UpdateAsync(steve);
            await _substitute.Received().ExecuteAsync("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33");
            await _substitute.Received().ExecuteAsync("EVAL", Scripts.JsonDiffResolution, "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33");
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public void TestUpdateJsonUnloadedScript()
        {
            _substitute.Execute("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);

            _substitute.Execute("EVALSHA", Arg.Any<object[]>())
                .Throws(new RedisServerException("Failed on EVALSHA"));
            _substitute.Execute("EVAL", Arg.Any<object[]>()).Returns(new RedisReply("42"));
            _substitute.Execute("SCRIPT", Arg.Any<object[]>()).Returns(new RedisReply("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb"));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            collection.Update(steve);
            _substitute.Received().Execute("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33");
            _substitute.Received().Execute("EVAL", Scripts.JsonDiffResolution, "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33");
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonName()
        {
            _substitute.ExecuteAsync("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);
            _substitute.ExecuteAsync("EVALSHA", Arg.Any<object[]>()).Returns(new RedisReply("42"));
            _substitute.ExecuteAsync("SCRIPT", Arg.Any<object[]>()).Returns(new RedisReply("42"));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = await collection.FirstAsync(x => x.Name == "Steve");
            steve.Name = "Bob";
            await collection.UpdateAsync(steve);
            await _substitute.Received().ExecuteAsync("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Name", "\"Bob\"");
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonNestedObject()
        {
            _substitute.ExecuteAsync("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);
            _substitute.ExecuteAsync("EVALSHA", Arg.Any<object[]>()).Returns(new RedisReply("42"));
            _substitute.ExecuteAsync("SCRIPT", Arg.Any<object[]>()).Returns(new RedisReply("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb"));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = await collection.FirstAsync(x => x.Name == "Steve");
            steve.Address = new Address { State = "Florida" };
            await collection.UpdateAsync(steve);
            var expected = $"{{{Environment.NewLine}  \"State\": \"Florida\"{Environment.NewLine}}}";
            await _substitute.Received().ExecuteAsync("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Address", expected);

            steve.Address.City = "Satellite Beach";
            await collection.UpdateAsync(steve);
            expected = "\"Satellite Beach\"";
            await _substitute.Received().ExecuteAsync("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Address.City", expected);

            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonWithDouble()
        {
            _substitute.ExecuteAsync("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);
            _substitute.ExecuteAsync("EVALSHA", Arg.Any<object[]>()).Returns(new RedisReply("42"));
            _substitute.ExecuteAsync("SCRIPT", Arg.Any<object[]>()).Returns(new RedisReply("42"));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = await collection.FirstAsync(x => x.Name == "Steve");
            steve.Age = 33;
            steve.Height = 71.5;
            await collection.UpdateAsync(steve);
            await _substitute.Received().ExecuteAsync("EVALSHA", Arg.Any<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33", "SET", "$.Height", "71.5");
            Scripts.ShaCollection.Clear();
        }

        [Theory]
        [InlineData("en-DE")]
        [InlineData("it-IT")]
        [InlineData("es-ES")]
        public void TestUpdateJsonWithDoubleTestingInvariantCultureCompliance(string lcid)
        {
            Helper.RunTestUnderDifferentCulture(lcid, async _ => await TestUpdateJsonWithDouble());
        }

        [Fact]
        public async Task TestDeleteAsync()
        {
            const string key = "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N";
            _substitute.ExecuteAsync("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);
            _substitute.ExecuteAsync("UNLINK", Arg.Any<object[]>()).Returns("1");
            var collection = new RedisCollection<Person>(_substitute);
            var steve = await collection.FirstAsync(x => x.Name == "Steve");
            Assert.True(collection.StateManager.Data.ContainsKey(key));
            Assert.True(collection.StateManager.Snapshot.ContainsKey(key));
            await collection.DeleteAsync(steve);
            await _substitute.Received().ExecuteAsync("UNLINK", key);
            Assert.False(collection.StateManager.Data.ContainsKey(key));
            Assert.False(collection.StateManager.Snapshot.ContainsKey(key));
        }

        [Fact]
        public void TestDelete()
        {
            const string key = "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N";
            _substitute.Execute("FT.SEARCH", Arg.Any<object[]>()).Returns(_mockReply);
            _substitute.Execute("UNLINK", Arg.Any<object[]>()).Returns(new RedisReply("1"));
            var collection = new RedisCollection<Person>(_substitute);
            var steve = collection.First(x => x.Name == "Steve");
            Assert.True(collection.StateManager.Data.ContainsKey(key));
            Assert.True(collection.StateManager.Snapshot.ContainsKey(key));
            collection.Delete(steve);
            _substitute.Received().Execute("UNLINK", key);
            Assert.False(collection.StateManager.Data.ContainsKey(key));
            Assert.False(collection.StateManager.Snapshot.ContainsKey(key));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstAsync(bool useExpression)
        {
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_substitute);
            if (useExpression)
            {
                _ = await collection.FirstAsync(x => x.TagField == "bob");
            }
            else
            {
                _ = await collection.FirstAsync();
            }

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstAsyncNone(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyNone);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_substitute);
            if (useExpression)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.FirstAsync(x => x.TagField == "bob"));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.FirstAsync());
            }
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstOrDefaultAsync(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_substitute);
            Person? res;
            if (useExpression)
            {
                res = await collection.FirstOrDefaultAsync(x => x.TagField == "bob");
            }
            else
            {
                res = await collection.FirstOrDefaultAsync();
            }

            Assert.NotNull(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstOrDefaultAsyncNone(bool useExpression)
        {
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyNone);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            Person? res;
            if (useExpression)
            {
                res = await collection.FirstOrDefaultAsync(x => x.TagField == "bob");
            }
            else
            {
                res = await collection.FirstOrDefaultAsync();
            }

            Assert.Null(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleAsync(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var collection = new RedisCollection<Person>(_substitute);
            Person res;
            if (useExpression)
            {
                res = await collection.SingleAsync(x => x.TagField == "bob");
            }
            else
            {
                res = await collection.SingleAsync();
            }
            Assert.NotNull(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleAsyncNone(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyNone);

            var collection = new RedisCollection<Person>(_substitute);
            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            if (useExpression)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync(x => x.TagField == "bob"));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync());
            }

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleAsyncTwo(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);
            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            if (useExpression)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync(x => x.TagField == "bob"));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync());
            }

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleOrDefaultAsync(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            Person? res;
            if (useExpression)
            {
                res = await collection.SingleOrDefaultAsync(x => x.TagField == "bob");
            }
            else
            {
                res = await collection.SingleOrDefaultAsync();
            }

            Assert.NotNull(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleOrDefaultAsyncNone(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyNone);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            Person? res;
            if (useExpression)
            {
                res = await collection.SingleOrDefaultAsync(x => x.TagField == "bob");
            }
            else
            {
                res = await collection.SingleOrDefaultAsync();
            }

            Assert.Null(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleOrDefaultAsyncTwo(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            Person? res;
            if (useExpression)
            {
                res = await collection.SingleOrDefaultAsync(x => x.TagField == "bob");
            }
            else
            {
                res = await collection.SingleOrDefaultAsync();
            }

            Assert.Null(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAnyAsync(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_substitute);

            var res = await (useExpression ? collection.AnyAsync(x => x.TagField == "bob") : collection.AnyAsync());
            Assert.True(res);
            
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAnyAsyncNone(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReplyNone);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var res = await (useExpression ? collection.AnyAsync(x => x.TagField == "bob") : collection.AnyAsync());

            Assert.False(res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestCountAsync(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var res = await (useExpression ? collection.CountAsync(x => x.TagField == "bob") : collection.CountAsync());
            Assert.Equal(1, res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestCount2Async(bool useExpression)
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);
            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var res = await (useExpression ? collection.CountAsync(x => x.TagField == "bob") : collection.CountAsync());
            Assert.Equal(2, res);
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0");
        }

        [Fact]
        public async Task TestOrderByWithAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);
            var collection = new RedisCollection<Person>(_substitute);
            var expectedPredicate = "*";
            _ = await collection.OrderBy(x => x.Age).ToListAsync();
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "100",
                "SORTBY",
                "Age",
                "ASC");
        }

        [Fact]
        public async Task TestOrderByDescendingWithAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);
            var collection = new RedisCollection<Person>(_substitute);
            var expectedPredicate = "*";
            _ = await collection.OrderByDescending(x => x.Age).ToListAsync();
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "100",
                "SORTBY",
                "Age",
                "DESC");
        }

        [Fact]
        public async Task CombinedExpressionsWithFirstOrDefaultAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").FirstOrDefaultAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public async Task CombinedExpressionsWithFirstAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").FirstAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public async Task CombinedExpressionsWithAnyAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").AnyAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0");
        }

        [Fact]
        public async Task CombinedExpressionsSingleOrDefaultAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").SingleOrDefaultAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public async Task CombinedExpressionsSingleAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").SingleAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public async Task CombinedExpressionsCountAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").CountAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0");
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionFirstOrDefaultAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply2Count);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).FirstOrDefaultAsync(x => x.Name == "Bob");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1",
                "GEOFILTER",
                "Home",
                "5",
                "5",
                "10",
                "mi");
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionFirstAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).FirstAsync(x => x.Name == "Bob");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1",
                "GEOFILTER",
                "Home",
                "5",
                "5",
                "10",
                "mi");
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionAnyAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).AnyAsync(x => x.Name == "Bob");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0",
                "GEOFILTER",
                "Home",
                "5",
                "5",
                "10",
                "mi");
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionSingleAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).SingleAsync(x => x.Name == "Bob");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1",
                "GEOFILTER",
                "Home",
                "5",
                "5",
                "10",
                "mi");
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionSingleOrDefaultAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).SingleOrDefaultAsync(x => x.Name == "Bob");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1",
                "GEOFILTER",
                "Home",
                "5",
                "5",
                "10",
                "mi");
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionCountAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).CountAsync(x => x.Name == "Bob");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0",
                "GEOFILTER",
                "Home",
                "5",
                "5",
                "10",
                "mi");
        }

        [Fact]
        public async Task TestCreateIndexWithNoStopwords()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns("OK");

            await _substitute.CreateIndexAsync(typeof(ObjectWithZeroStopwords));

            await _substitute.Received().ExecuteAsync(
                "FT.CREATE",
                $"{nameof(ObjectWithZeroStopwords).ToLower()}-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ObjectWithZeroStopwords)}:",
                "STOPWORDS",
                "0",
                "SCHEMA", "Name", "TAG", "SEPARATOR", "|");
        }

        [Fact]
        public async Task TestCreateIndexWithTwoStopwords()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns("OK");

            await _substitute.CreateIndexAsync(typeof(ObjectWithTwoStopwords));

            await _substitute.Received().ExecuteAsync(
                "FT.CREATE",
                $"{nameof(ObjectWithTwoStopwords).ToLower()}-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ObjectWithTwoStopwords)}:",
                "STOPWORDS", "2", "foo", "bar",
                "SCHEMA", "Name", "TAG", "SEPARATOR", "|");
        }

        [Fact]
        public async Task TestCreateIndexWithStringLikeValueTypes()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns("OK");

            await _substitute.CreateIndexAsync(typeof(ObjectWithStringLikeValueTypes));

            await _substitute.Received().ExecuteAsync("FT.CREATE",
                "objectwithstringlikevaluetypes-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithStringLikeValueTypes:",
                "SCHEMA",
                "$.Ulid", "AS", "Ulid", "TAG", "SEPARATOR", "|",
                "$.Boolean", "AS", "Boolean", "TAG", "SEPARATOR", "|",
                "$.Guid", "AS", "Guid", "TAG", "SEPARATOR", "|",
                "$.AnEnum", "AS", "AnEnum", "TAG",
                "$.AnEnumAsInt", "AS", "AnEnumAsInt", "NUMERIC",
                "$.Flags", "AS", "Flags", "TAG", "SEPARATOR", ","
            );
        }

        [Fact]
        public async Task TestCreateIndexWithStringLikeValueTypesHash()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns("OK");

            await _substitute.CreateIndexAsync(typeof(ObjectWithStringLikeValueTypesHash));

            await _substitute.Received().ExecuteAsync("FT.CREATE",
                "objectwithstringlikevaluetypeshash-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithStringLikeValueTypesHash:",
                "SCHEMA",
                "Ulid",
                "TAG", "SEPARATOR", "|",
                "Boolean",
                "TAG", "SEPARATOR", "|", "Guid", "TAG", "SEPARATOR", "|", "AnEnum", "NUMERIC"
            );
        }

        [Fact]
        public async Task TestCreateIndexWithDatetimeValue()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns("OK");

            await _substitute.CreateIndexAsync(typeof(ObjectWithDateTime));
            await _substitute.CreateIndexAsync(typeof(ObjectWithDateTimeHash));

            await _substitute.Received().ExecuteAsync("FT.CREATE",
                "objectwithdatetime-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithDateTime:",
                "SCHEMA",
                "$.Timestamp", "AS", "Timestamp", "NUMERIC", "SORTABLE",
                "$.NullableTimestamp", "AS", "NullableTimestamp", "NUMERIC"
            );

            await _substitute.Received().ExecuteAsync("FT.CREATE",
                "objectwithdatetimehash-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithDateTimeHash:",
                "SCHEMA",
                "Timestamp", "NUMERIC",
                "NullableTimestamp", "NUMERIC"
            );
        }

        [Fact]
        public async Task TestQueryOfUlid()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute);

            var ulid = Ulid.NewUlid();

            _ = await collection.Where(x => x.Ulid == ulid).ToListAsync();
            var expectedPredicate = $"(@Ulid:{{{ulid}}})";

            await _substitute.Received().ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100");
        }

        [Fact]
        public async Task TestQueryOfGuid()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute);

            var guid = Guid.NewGuid();

            _ = await collection.Where(x => x.Guid == guid).ToListAsync();

            var expectedPredicate = $"(@Guid:{{{ExpressionParserUtilities.EscapeTagField(guid.ToString())}}})";

            await _substitute.Received().ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100");
        }

        [Fact]
        public async Task TestQueryOfBoolean()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute);

            const bool boolean = true;

            _ = await collection.Where(x => x.Boolean == true).ToListAsync();

            var expectedPredicate = $"(@Boolean:{{{boolean}}})";

            await _substitute.Received().ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100");
        }

        [Fact]
        public async Task TestQueryOfEnum()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute);

            var anEnum = AnEnum.two;

            _ = await collection.Where(x => x.AnEnum == AnEnum.two && x.AnEnumAsInt == anEnum).ToListAsync();

            var expectedPredicate = $"((@AnEnum:{{{AnEnum.two}}}) (@AnEnumAsInt:[1 1]))";

            await _substitute.Received().ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100");
        }

        [Fact]
        public async Task TestQueryOfEnumHash()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypesHash>(_substitute);
            
            _ = await collection.Where(x => x.AnEnum == AnEnum.two).ToListAsync();

            var expectedPredicate = $"(@AnEnum:[1 1])";

            await _substitute.Received().ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypeshash-idx", expectedPredicate, "LIMIT", "0", "100");
        }

        [Fact]
        public async Task TestGreaterThanEnumQuery()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute);

            _ = await collection.Where(x => (int)x.AnEnumAsInt > 1).ToListAsync();

            var expectedPredicate = "(@AnEnumAsInt:[(1 inf])";

            await _substitute.Received().ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100");
        }

        [Fact]
        public async Task TestIndexCreationWithEmbeddedListOfDocuments()
        {
            _substitute.ExecuteAsync("FT.CREATE", Arg.Any<object[]>()).Returns("OK");
            await _substitute.CreateIndexAsync(typeof(ObjectWithEmbeddedArrayOfObjects));
            await _substitute.Received().ExecuteAsync("FT.CREATE",
                "objectwithembeddedarrayofobjects-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithEmbeddedArrayOfObjects:",
                "SCHEMA",
                "$.Addresses[*].City", "AS", "Addresses_City", "TAG", "SEPARATOR", "|",
                "$.Addresses[*].State", "AS", "Addresses_State", "TAG", "SEPARATOR", "|",
                "$.Addresses[*].AddressType", "AS", "Addresses_AddressType", "TAG",
                "$.Addresses[*].Boolean", "AS", "Addresses_Boolean", "TAG", "SEPARATOR", "|",
                "$.Addresses[*].Guid", "AS", "Addresses_Guid", "TAG", "SEPARATOR", "|",
                "$.Addresses[*].Ulid", "AS", "Addresses_Ulid", "TAG", "SEPARATOR", "|",
                "$.AddressList[*].City", "AS", "AddressList_City", "TAG", "SEPARATOR", "|",
                "$.AddressList[*].State", "AS", "AddressList_State", "TAG", "SEPARATOR", "|",
                "$.Name", "AS", "Name", "TAG", "SEPARATOR", "|", "$.Numeric", "AS", "Numeric", "NUMERIC");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjects()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach")).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "(@Addresses_City:{Satellite\\ Beach})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsEnum()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.Addresses.Any(a => a.AddressType == AddressType.Home)).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "(@Addresses_AddressType:{Home})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsExtraPredicate()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
               x.Numeric == 100 || x.Name == "Bob" && x.Addresses.Any(a => a.City == "Satellite Beach")).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Numeric:[100 100]) | ((@Name:{Bob}) (@Addresses_City:{Satellite\\ Beach})))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsMultipleAnyCalls()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach") && x.AddressList.Any(y => y.City == "Newark")).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Addresses_City:{Satellite\\ Beach}) (@AddressList_City:{Newark}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsMultiplePredicatesInsideAny()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach" && a.State == "Florida")).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Addresses_City:{Satellite\\ Beach}) (@Addresses_State:{Florida}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsOtherTypes()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var boolean = true;
            var ulid = Ulid.NewUlid();
            var guid = Guid.NewGuid();

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.Addresses.Any(a => a.Ulid == ulid) && x.Addresses.Any(a => a.Guid == guid) && x.Addresses.Any(a => a.Boolean == boolean)).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                $"(((@Addresses_Ulid:{{{ulid}}}) (@Addresses_Guid:{{{ExpressionParserUtilities.EscapeTagField(guid.ToString())}}})) (@Addresses_Boolean:{{{boolean}}}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForListOfEmbeddedObjects()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.AddressList.Any(a => a.City == "Satellite Beach")).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "(@AddressList_City:{Satellite\\ Beach})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsMultiVariant()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_substitute);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach" && a.State == "Florida")).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Addresses_City:{Satellite\\ Beach}) (@Addresses_State:{Florida}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task SearchWithMultipleWhereClauses()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);

            await collection
                .Where(x => x.Name == "steve")
                .Where(x => x.Age == 32)
                .Where(x => x.TagField == "foo").ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "(((@Name:\"steve\") (@Age:[32 32])) (@TagField:{foo}))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public async Task TestAsyncMaterializationMethodsWithCombinedQueries()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            _ = await collection.Where(x => x.TagField == "CountAsync")
                .CountAsync(x => x.Age == 32);
            _ = await collection.Where(x => x.TagField == "AnyAsync")
                .AnyAsync(x => x.Age == 32);
            _ = await collection.Where(x => x.TagField == "SingleAsync")
                .SingleAsync(x => x.Age == 32);
            _ = await collection.Where(x => x.TagField == "SingleOrDefaultAsync")
                .SingleOrDefaultAsync(x => x.Age == 32);
            _ = await collection.Where(x => x.TagField == "FirstAsync")
                .FirstAsync(x => x.Age == 32);
            _ = await collection.Where(x => x.TagField == "FirstOrDefaultAsync")
                .FirstOrDefaultAsync(x => x.Age == 32);

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{CountAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "0");
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{AnyAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "0");
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{SingleAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1");
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{FirstAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1");

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{SingleOrDefaultAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1");
            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{FirstOrDefaultAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public void TestMaterializationMethodsWithCombinedQueries()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => x.Age == 32);
            _ = collection.Count(x => x.TagField == "Count");
            _ = collection.Any(x => x.TagField == "Any");
            _ = collection.Single(x => x.TagField == "Single");
            _ = collection.SingleOrDefault(x => x.TagField == "SingleOrDefault");
            _ = collection.First(x => x.TagField == "First");
            _ = collection.FirstOrDefault(x => x.TagField == "FirstOrDefault");

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{Count}))",
                "LIMIT",
                "0",
                "0");
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{Any}))",
                "LIMIT",
                "0",
                "0");
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{Single}))",
                "LIMIT",
                "0",
                "1");
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{First}))",
                "LIMIT",
                "0",
                "1");

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{SingleOrDefault}))",
                "LIMIT",
                "0",
                "1");
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{FirstOrDefault}))",
                "LIMIT",
                "0",
                "1");
        }

        [Fact]
        public void SearchTagFieldContains()
        {
            var potentialTagFieldValues = new [] { "Steve", "Alice", "Bob" };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => potentialTagFieldValues.Contains(x.TagField));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{Steve|Alice|Bob})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchTextFieldContains()
        {
            var potentialTextFieldValues = new [] { "Steve", "Alice", "Bob" };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => potentialTextFieldValues.Contains(x.Name));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Steve|Alice|Bob)",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchNumericFieldContains()
        {
            var potentialTagFieldValues = new int?[] { 35, 50, 60 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => potentialTagFieldValues.Contains(x.Age));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "@Age:[35 35]|@Age:[50 50]|@Age:[60 60]",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void Issue201()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var p1 = new Person() { Name = "Steve" };
            var collection = new RedisCollection<Person>(_substitute, 1000);
            _ = collection.Where(x => x.NickNames.Contains(p1.Name)).ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve})",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void RangeOnDateTimeWithMultiplePredicates()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var fromDto = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(4));
            var toDto = DateTimeOffset.UtcNow;

            var fromDt = fromDto.DateTime;
            var toDt = toDto.DateTime;

            var msFrom = fromDto.ToUnixTimeMilliseconds();
            var msTo = toDto.ToUnixTimeMilliseconds();

            var collection = new RedisCollection<ObjectWithDateTime>(_substitute, 1000);
            _ = collection.Where(x => x.TimestampOffset <= fromDto && x.TimestampOffset >= toDto).ToList();
            _ = collection.Where(x => x.TimestampOffset <= fromDt && x.TimestampOffset >= toDt).ToList();
            _ = collection.Where(x => x.Timestamp <= fromDto && x.Timestamp >= toDto).ToList();
            _ = collection.Where(x => x.Timestamp <= fromDt && x.Timestamp >= toDt).ToList();
            _ = collection.Where(x => x.NullableTimestamp <= fromDto && x.NullableTimestamp >= toDto).ToList();
            _ = collection.Where(x => x.NullableTimestamp <= fromDt && x.NullableTimestamp >= toDt).ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"((@TimestampOffset:[-inf {msFrom}]) (@TimestampOffset:[{msTo} inf]))",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"((@TimestampOffset:[-inf {new DateTimeOffset(fromDt).ToUnixTimeMilliseconds()}]) (@TimestampOffset:[{new DateTimeOffset(toDt).ToUnixTimeMilliseconds()} inf]))",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"((@Timestamp:[-inf {msFrom}]) (@Timestamp:[{msTo} inf]))",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"((@Timestamp:[-inf {new DateTimeOffset(fromDt).ToUnixTimeMilliseconds()}]) (@Timestamp:[{new DateTimeOffset(toDt).ToUnixTimeMilliseconds()} inf]))",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"((@NullableTimestamp:[-inf {msFrom}]) (@NullableTimestamp:[{msTo} inf]))",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"((@NullableTimestamp:[-inf {new DateTimeOffset(fromDt).ToUnixTimeMilliseconds()}]) (@NullableTimestamp:[{new DateTimeOffset(toDt).ToUnixTimeMilliseconds()} inf]))",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void RangeOnDatetime()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var timestamp = DateTime.Now;
            var timeAnHourAgo = timestamp.Subtract(TimeSpan.FromHours(1));
            var timeAnHourAgoMilliseconds = new DateTimeOffset(timeAnHourAgo).ToUnixTimeMilliseconds();
            DateTime? timeTwoHoursAgoNullable = timestamp.Subtract(TimeSpan.FromHours(2));
            var timeTwoHoursAgoMilliseconds = new DateTimeOffset(timeTwoHoursAgoNullable.Value).ToUnixTimeMilliseconds();

            var collection = new RedisCollection<ObjectWithDateTime>(_substitute, 1000);

            var mockObj = new ObjectWithDateTime { Timestamp = timestamp.Subtract(TimeSpan.FromHours(3)) };
            var timeThreeHoursAgoMilliseconds = new DateTimeOffset(mockObj.Timestamp).ToUnixTimeMilliseconds();
            _ = collection.Where(x => x.Timestamp == timeAnHourAgo).ToList();
            _ = collection.Where(x => x.Timestamp > timeAnHourAgo).ToList();
            _ = collection.Where(x => x.NullableTimestamp > timeAnHourAgo).ToList();

            _ = collection.Where(x => x.Timestamp > timeTwoHoursAgoNullable).ToList();
            _ = collection.Where(x => x.NullableTimestamp > timeTwoHoursAgoNullable).ToList();

            _ = collection.Where(x => x.Timestamp > mockObj.Timestamp).ToList();
            _ = collection.Where(x => x.NullableTimestamp > mockObj.Timestamp).ToList();

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public async Task RangeOnDatetimeAsync()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var timestamp = DateTime.Now;
            var timeAnHourAgo = timestamp.Subtract(TimeSpan.FromHours(1));
            DateTime? timeTwoHoursAgoNullable = timestamp.Subtract(TimeSpan.FromHours(2));
            var timeTwoHoursAgoMilliseconds = new DateTimeOffset(timeTwoHoursAgoNullable.Value).ToUnixTimeMilliseconds();
            var timeAnHourAgoMilliseconds = new DateTimeOffset(timeAnHourAgo).ToUnixTimeMilliseconds();
            var collection = new RedisCollection<ObjectWithDateTime>(_substitute, 1000);

            var mockObj = new ObjectWithDateTime { Timestamp = timestamp.Subtract(TimeSpan.FromHours(3)) };
            var timeThreeHoursAgoMilliseconds = new DateTimeOffset(mockObj.Timestamp).ToUnixTimeMilliseconds();
            _ = await collection.Where(x => x.Timestamp == timeAnHourAgo).ToListAsync();
            _ = await collection.Where(x => x.Timestamp > timeAnHourAgo).ToListAsync();
            _ = await collection.Where(x => x.NullableTimestamp > timeAnHourAgo).ToListAsync();

            _ = await collection.Where(x => x.Timestamp > timeTwoHoursAgoNullable).ToListAsync();
            _ = await collection.Where(x => x.NullableTimestamp > timeTwoHoursAgoNullable).ToListAsync();

            _ = await collection.Where(x => x.Timestamp > mockObj.Timestamp).ToListAsync();
            _ = await collection.Where(x => x.NullableTimestamp > mockObj.Timestamp).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public async Task RangeOnDatetimeAsyncHash()
        {
            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var timestamp = DateTime.Now;
            var timeAnHourAgo = timestamp.Subtract(TimeSpan.FromHours(1));
            DateTime? timeTwoHoursAgoNullable = timestamp.Subtract(TimeSpan.FromHours(2));
            var timeTwoHoursAgoMilliseconds = new DateTimeOffset(timeTwoHoursAgoNullable.Value).ToUnixTimeMilliseconds();
            var timeAnHourAgoMilliseconds = new DateTimeOffset(timeAnHourAgo).ToUnixTimeMilliseconds();
            var collection = new RedisCollection<ObjectWithDateTimeHash>(_substitute, 1000);

            var mockObj = new ObjectWithDateTimeHash { Timestamp = timestamp.Subtract(TimeSpan.FromHours(3)) };
            var timeThreeHoursAgoMilliseconds = new DateTimeOffset(mockObj.Timestamp).ToUnixTimeMilliseconds();
            await collection.Where(x => x.Timestamp == timeAnHourAgo).ToListAsync();
            await collection.Where(x => x.Timestamp == timeAnHourAgo).OrderBy(x => x.Timestamp).ToListAsync();
            await collection.Where(x => x.Timestamp > timeAnHourAgo).ToListAsync();
            await collection.Where(x => x.NullableTimestamp > timeAnHourAgo).ToListAsync();

            await collection.Where(x => x.Timestamp > timeTwoHoursAgoNullable).ToListAsync();
            await collection.Where(x => x.NullableTimestamp > timeTwoHoursAgoNullable).ToListAsync();

            await collection.Where(x => x.Timestamp > mockObj.Timestamp).ToListAsync();
            await collection.Where(x => x.NullableTimestamp > mockObj.Timestamp).ToListAsync();

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000",
                "SORTBY",
                "Timestamp",
                "ASC"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@NullableTimestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@NullableTimestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );

            await _substitute.Received().ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@NullableTimestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            );
        }

        [Fact]
        public void SearchNumericFieldListContains()
        {
            var potentialTagFieldValues = new List<int?> { 35, 50, 60 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => potentialTagFieldValues.Contains(x.Age));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "@Age:[35 35]|@Age:[50 50]|@Age:[60 60]",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchTagFieldAndTextListContains()
        {
            var potentialTagFieldValues = new List<string> { "Steve", "Alice", "Bob" };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => potentialTagFieldValues.Contains(x.TagField) || potentialTagFieldValues.Contains(x.Name));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{Steve|Alice|Bob}) | (@Name:Steve|Alice|Bob))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestNullResponseDoc()
        {
            int? nullVal = null;
            var nullResult = RedisResult.Create(nullVal);
            var res = new RedisReply[] { 1, $"foo:{Ulid.NewUlid()}", new(nullResult) };

            var query = new RedisQuery("fake-idx");
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>())
                .Returns((RedisReply)res);

            _ = _substitute.Search<Person>(query);
        }

        [Fact]
        public void SearchTagFieldAndTextListContainsWithEscapes()
        {
            var potentialTagFieldValues = new List<string> { "steve@example.com", "alice@example.com", "bob@example.com" };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute).Where(x => potentialTagFieldValues.Contains(x.TagField) || potentialTagFieldValues.Contains(x.Name));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{steve\\@example\\.com|alice\\@example\\.com|bob\\@example\\.com}) | (@Name:steve@example.com|alice@example.com|bob@example.com))",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchWithEmptyAny()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            var any = collection.Any();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "0");
            Assert.True(any);

            any = collection.Any(x => x.TagField == "foo");
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{foo})",
                "LIMIT",
                "0",
                "0");

            Assert.True(any);
        }

        [Fact]
        public void TestContainsFromLocal()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            const string steve = "steve";
            _ = collection.Where(x => x.NickNamesList.Contains(steve)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNamesList:{steve})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchGuidFieldContains()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();
            var guid1Str = ExpressionParserUtilities.EscapeTagField(guid1.ToString());
            var guid2Str = ExpressionParserUtilities.EscapeTagField(guid2.ToString());
            var guid3Str = ExpressionParserUtilities.EscapeTagField(guid3.ToString());
            var potentialFieldValues = new [] { guid1, guid2, guid3 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.Contains(x.Guid));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@Guid:{{{guid1Str}|{guid2Str}|{guid3Str}}})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestContainsFromProperty()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            var steve = new Person
            {
                Name = "steve"
            };
            _ = collection.Where(x => x.NickNamesList.Contains(steve.Name)).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNamesList:{steve})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchUlidFieldContains()
        {
            var ulid1 = Ulid.NewUlid();
            var ulid2 = Ulid.NewUlid();
            var ulid3 = Ulid.NewUlid();

            var potentialFieldValues = new [] { ulid1, ulid2, ulid3 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.Contains(x.Ulid));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@Ulid:{{{ulid1}|{ulid2}|{ulid3}}})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchEnumFieldContains()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new [] { enum1, enum2, enum3 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.Contains(x.AnEnum));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@AnEnum:{{one|two|three}})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchNumericEnumFieldContains()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new [] { enum1, enum2, enum3 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.Contains(x.AnEnumAsInt));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                "@AnEnumAsInt:[0 0]|@AnEnumAsInt:[1 1]|@AnEnumAsInt:[2 2]",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchEnumFieldContainsList()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new List<AnEnum> { enum1, enum2, enum3 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.Contains(x.AnEnum));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@AnEnum:{{one|two|three}})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchNumericEnumFieldContainsList()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new List<AnEnum> { enum1, enum2, enum3 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.Contains(x.AnEnumAsInt));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                "@AnEnumAsInt:[0 0]|@AnEnumAsInt:[1 1]|@AnEnumAsInt:[2 2]",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchEnumFieldContainsListAsProperty()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new { list = new List<AnEnum> { enum1, enum2, enum3 } };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.list.Contains(x.AnEnum));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@AnEnum:{{one|two|three}})",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void SearchNumericEnumFieldContainsListAsProperty()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new { list = new List<AnEnum> { enum1, enum2, enum3 } };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_substitute).Where(x => potentialFieldValues.list.Contains(x.AnEnumAsInt));
            _ = collection.ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                "@AnEnumAsInt:[0 0]|@AnEnumAsInt:[1 1]|@AnEnumAsInt:[2 2]",
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestNestedOrderBy()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            _ = new RedisCollection<Person>(_substitute).OrderBy(x => x.Address.State).ToList();
            _substitute.Received().Execute("FT.SEARCH", "person-idx", "*", "LIMIT", "0", "100", "SORTBY", "Address_State", "ASC");
        }

        [Fact]
        public void TestGeoFilterNested()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.GeoFilter(x => x.Address.Location, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "GEOFILTER",
                "Address_Location",
                "5",
                "6.7",
                "50",
                "km"
            );
        }

        [Fact]
        public void TestSelectWithWhere()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.Age == 33).Select(x => x.Name).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "Name");
        }

        [Fact]
        public void TestNullableEnumQueries()

        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithNullableEnum>(_substitute);
            _ = collection.Where(x => x.AnEnum == AnEnum.one && x.NullableStringEnum == AnEnum.two).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnullableenum-idx",
                "((@AnEnum:[0 0]) (@NullableStringEnum:{two}))",
                "LIMIT",
                "0",
                "100"
            );
        }

        [Fact]
        public void TestEscapeForwardSlash()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Where(x => x.TagField == "a/test/string").ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{a\\/test\\/string})",
                "LIMIT",
                "0",
                "100"
            );
        }

        [Fact]
        public void TestMixedNestingIndexCreation()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply("OK"));

            _substitute.CreateIndex(typeof(ComplexObjectWithCascadeAndJsonPath));

            _substitute.Received().Execute(
                "FT.CREATE",
                $"{nameof(ComplexObjectWithCascadeAndJsonPath).ToLower()}-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ComplexObjectWithCascadeAndJsonPath)}:",
                "SCHEMA", "$.InnerCascade.InnerInnerJson.Tag", "AS", "InnerCascade_InnerInnerJson_Tag", "TAG", "SEPARATOR", "|",
                "$.InnerCascade.InnerInnerCascade.Tag", "AS", "InnerCascade_InnerInnerCascade_Tag", "TAG", "SEPARATOR", "|",
                "$.InnerCascade.InnerInnerCascade.Num", "AS", "InnerCascade_InnerInnerCascade_Num", "NUMERIC",
                "$.InnerCascade.InnerInnerCascade.Arr[*]", "AS", "InnerCascade_InnerInnerCascade_Arr", "TAG", "SEPARATOR", "|",
                "$.InnerCascade.InnerInnerCollection[*].Tag", "AS", "InnerCascade_InnerInnerCollection_Tag", "TAG", "SEPARATOR", "|",
                "$.InnerJson.InnerInnerCascade.Tag", "AS", "InnerJson_InnerInnerCascade_Tag", "TAG", "SEPARATOR", "|",
                "$.InnerJson.InnerInnerCascade.Num", "AS", "InnerJson_InnerInnerCascade_Num", "NUMERIC",
                "$.InnerJson.InnerInnerCascade.Arr[*]", "AS", "InnerJson_InnerInnerCascade_Arr", "TAG", "SEPARATOR", "|");
        }

        [Fact]
        public void TestMixedNestingQuerying()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var collection = new RedisCollection<ComplexObjectWithCascadeAndJsonPath>(_substitute);

            _ = collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCascade.Tag == "hello");
            _substitute.Received().Execute(
                "FT.SEARCH",
                "complexobjectwithcascadeandjsonpath-idx",
                "(@InnerCascade_InnerInnerCascade_Tag:{hello})",
                "LIMIT",
                "0",
                "1"
            );

            _ = collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCascade.Num == 5);
            _substitute.Received().Execute(
                "FT.SEARCH",
                "complexobjectwithcascadeandjsonpath-idx",
                "(@InnerCascade_InnerInnerCascade_Num:[5 5])",
                "LIMIT",
                "0",
                "1"
            );

            _ = collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCollection.Any(y => y.Tag == "hello"));
            _substitute.Received().Execute(
                "FT.SEARCH",
                "complexobjectwithcascadeandjsonpath-idx",
                "(@InnerCascade_InnerInnerCollection_Tag:{hello})",
                "LIMIT",
                "0",
                "1"
            );
        }
        
        [Fact]
        public async Task TestCreateIndexWithJsonPropertyName()
        {
            _substitute.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns("OK");

            await _substitute.CreateIndexAsync(typeof(ObjectWithPropertyNamesDefined));

            await _substitute.Received().ExecuteAsync(
                "FT.CREATE",
                $"{nameof(ObjectWithPropertyNamesDefined).ToLower()}-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ObjectWithPropertyNamesDefined)}:",
                "SCHEMA", "$.notKey", "AS", "notKey", "TAG", "SEPARATOR", "|");
        }

        [Fact]
        public void QueryNamedPropertiesJson()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithPropertyNamesDefined>(_substitute);

            _ = collection.FirstOrDefault(x => x.Key == "hello");

            _substitute.Received().Execute(
                "FT.SEARCH",
                $"{nameof(ObjectWithPropertyNamesDefined).ToLower()}-idx",
                "(@notKey:{hello})",
                "LIMIT",
                "0",
                "1"
            );
        }
        
        [Fact]
        public void TestMultipleContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithMultipleSearchableFields>(_substitute);
            Expression<Func<ObjectWithMultipleSearchableFields, bool>> whereExpressionFail = a => !a.FirstName.Contains("Andrey") && !a.LastName.Contains("Bred");

            _ = collection.Where(whereExpressionFail).ToList();
            whereExpressionFail = a => !a.FirstName.Contains("Andrey") && a.LastName.Contains("Bred");
            _ = collection.Where(whereExpressionFail).ToList();
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithmultiplesearchablefields-idx",
                "(-(@FirstName:Andrey) -(@LastName:Bred))",
                "LIMIT",
                "0",
                "100"
            );
            
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithmultiplesearchablefields-idx",
                "(-(@FirstName:Andrey) (@LastName:Bred))",
                "LIMIT",
                "0",
                "100"
            );
        }

        [Fact]
        public void TestSelectNestedObject()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            
            var collection = new RedisCollection<Person>(_substitute);
            _ = collection.Select(x => x.Address).ToList();
            _ = collection.Select(x => x.Address.ForwardingAddress).ToList();
            
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "$.Address"
            );
            
            _substitute.Received().Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "$.Address.ForwardingAddress"
            );
        }

        [Fact]
        public void NonNullableNumericFieldContains()
        {
            var ints = new [] { 1, 2, 3 };
            var bytes = new byte[] { 4, 5, 6 };
            var sbytes = new sbyte[] { 7, 8, 9 };
            var shorts = new short[] { 10, 11, 12 };
            var uints = new uint[] { 13, 14, 15 };
            var longs = new long[] { 16, 17, 18 };
            var ulongs = new ulong[] { 19, 20, 21 };
            var doubles = new [] { 22.5, 23, 24 };
            var floats = new [] { 25.5F, 26, 27 };
            var ushorts = new ushort[] { 28, 29, 30 };
            var decimals = new decimal[] { 31.5M, 32, 33 };
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => ints.Contains(x.Integer));
            _ = collection.ToList();
            var expected = $"@{nameof(ObjectWithNumerics.Integer)}:[1 1]|@{nameof(ObjectWithNumerics.Integer)}:[2 2]|@{nameof(ObjectWithNumerics.Integer)}:[3 3]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => bytes.Contains(x.Byte));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Byte)}:[4 4]|@{nameof(ObjectWithNumerics.Byte)}:[5 5]|@{nameof(ObjectWithNumerics.Byte)}:[6 6]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => sbytes.Contains(x.SByte));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.SByte)}:[7 7]|@{nameof(ObjectWithNumerics.SByte)}:[8 8]|@{nameof(ObjectWithNumerics.SByte)}:[9 9]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => shorts.Contains(x.Short));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Short)}:[10 10]|@{nameof(ObjectWithNumerics.Short)}:[11 11]|@{nameof(ObjectWithNumerics.Short)}:[12 12]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => ushorts.Contains(x.UShort));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.UShort)}:[28 28]|@{nameof(ObjectWithNumerics.UShort)}:[29 29]|@{nameof(ObjectWithNumerics.UShort)}:[30 30]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => uints.Contains(x.UInt));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.UInt)}:[13 13]|@{nameof(ObjectWithNumerics.UInt)}:[14 14]|@{nameof(ObjectWithNumerics.UInt)}:[15 15]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => longs.Contains(x.Long));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Long)}:[16 16]|@{nameof(ObjectWithNumerics.Long)}:[17 17]|@{nameof(ObjectWithNumerics.Long)}:[18 18]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => ulongs.Contains(x.ULong));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.ULong)}:[19 19]|@{nameof(ObjectWithNumerics.ULong)}:[20 20]|@{nameof(ObjectWithNumerics.ULong)}:[21 21]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => doubles.Contains(x.Double));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Double)}:[22.5 22.5]|@{nameof(ObjectWithNumerics.Double)}:[23 23]|@{nameof(ObjectWithNumerics.Double)}:[24 24]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
            
            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => floats.Contains(x.Float));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Float)}:[25.5 25.5]|@{nameof(ObjectWithNumerics.Float)}:[26 26]|@{nameof(ObjectWithNumerics.Float)}:[27 27]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");

            collection = new RedisCollection<ObjectWithNumerics>(_substitute).Where(x => decimals.Contains(x.Decimal));
            _ = collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Decimal)}:[31.5 31.5]|@{nameof(ObjectWithNumerics.Decimal)}:[32 32]|@{nameof(ObjectWithNumerics.Decimal)}:[33 33]";
            _substitute.Received().Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100");
        }

        [Fact]
        public void TestConstantExpressionContains()
        {
            _substitute.ClearSubstitute();
            _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);
            var collection = new RedisCollection<Person>(_substitute);
            var parameter = Expression.Parameter(typeof(Person), "b");
            var property = Expression.Property(parameter, "TagField");
            var values = new string[] { "James", "Bond" };
            MethodInfo contains = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.Name.Contains(nameof(Enumerable.Contains))).Single(x => x.GetParameters().Length == 2).MakeGenericMethod(property.Type);
            var body = Expression.Call(contains, Expression.Constant(values), property);
            var lambada = Expression.Lambda<Func<Person, bool>>(body, parameter);
            _ = collection.Where(lambada).ToList();
            _substitute.Received().Execute("FT.SEARCH", "person-idx", "(@TagField:{James|Bond})", "LIMIT", "0", "100");
        }

        [Fact]
        public async Task EnumerateAllWhenKeyExpires()
        {
            RedisReply firstReply = new RedisReply[]
            {
                new(2),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:E912BED67BD64386B4FDC7322D"),
                new(new RedisReply[]
                {
                    "$",
                    "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"E912BED67BD64386B4FDC7322D\"}"
                }),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                // Key expired while executing the search
                new(Array.Empty<RedisReply>())
            };
            RedisReply secondReply = new RedisReply[]
            {
                new(2),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:4F6AE0A9BAE044E4B2D2186044"),
                new(new RedisReply[]
                {
                    "$",
                    "{\"Name\":\"Josh\",\"Age\":30,\"Height\":12.0, \"Id\":\"4F6AE0A9BAE044E4B2D2186044\"}"
                })
            };
            RedisReply finalEmptyResult = new RedisReply[]
            {
                new(0),
            };

            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "2").Returns(firstReply);
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "2",
                "2").Returns(secondReply);
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "4",
                "2").Returns(finalEmptyResult);

            var people = new List<Person>();
            // Chunk size 2 induces the iterator to call FT.SEARCH 3 times
            await foreach (var person in new RedisCollection<Person>(_substitute, 2))
            {
                people.Add(person);
            }

            Assert.Equal(2, people.Count);

            Assert.Equal("Steve", people[0].Name);
            Assert.Equal("Josh", people[1].Name);
        }

        [Fact]
        public async Task EnumerateAllWhenKeyExpiresAtEnd()
        {
            RedisReply firstReply = new RedisReply[]
            {
                new(2),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:E912BED67BD64386B4FDC7322D"),
                new(new RedisReply[]
                {
                    "$",
                    "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"E912BED67BD64386B4FDC7322D\"}"
                }),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:4F6AE0A9BAE044E4B2D2186044"),
                new(new RedisReply[]
                {
                    "$",
                    "{\"Name\":\"Josh\",\"Age\":30,\"Height\":12.0, \"Id\":\"4F6AE0A9BAE044E4B2D2186044\"}"
                })
            };
            RedisReply secondReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                // Key expired while executing the search
                new(Array.Empty<RedisReply>())
            };
            RedisReply finalEmptyResult = new RedisReply[]
            {
                new(0),
            };

            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "2").Returns(firstReply);
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "2",
                "2").Returns(secondReply);
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "4",
                "2").Returns(finalEmptyResult);

            var people = new List<Person>();
            // Chunk size 2 induces the iterator to call FT.SEARCH 3 times
            await foreach (var person in new RedisCollection<Person>(_substitute, 2))
            {
                people.Add(person);
            }

            Assert.Equal(2, people.Count);

            Assert.Equal("Steve", people[0].Name);
            Assert.Equal("Josh", people[1].Name);
        }

        [Fact]
        public async Task EnumerateAllButAllExpired()
        {
            RedisReply firstReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                // Key expired while executing the search
                new(Array.Empty<RedisReply>())
            };
            RedisReply finalEmptyResult = new RedisReply[]
            {
                new(0),
            };

            _substitute.ClearSubstitute();
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "2").Returns(firstReply);
            _substitute.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "4",
                "2").Returns(finalEmptyResult);

            var people = new List<Person>();
            // Chunk size 2 induces the iterator to call FT.SEARCH twice
            await foreach (var person in new RedisCollection<Person>(_substitute, 2))
            {
                people.Add(person);
            }

            Assert.Empty(people);
        }
    }
}