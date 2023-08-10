using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Redis.OM.Common;
using Redis.OM.Extensions;
using Xunit;
using Redis.OM.Searching.Query;
using StackExchange.Redis;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class SearchTests
    {
        private Mock<IRedisConnection> _mock = new Mock<IRedisConnection>();

        private RedisReply _mockReply = new RedisReply[]
        {
            new RedisReply(1),
            new RedisReply("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new RedisReply(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            })
        };

        private RedisReply _mockReplyNone = new RedisReply[]
        {
            new (0),
        };

        private RedisReply _mockReply2Count = new RedisReply[]
        {
            new RedisReply(2),
            new RedisReply("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new RedisReply(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            })
        };

        private RedisReply _mockReplySelect = new RedisReply[]
        {
            new RedisReply(1),
            new RedisReply("Person:33b58265-2656-4c5e-8476-7246549797d1"),
            new RedisReply(new RedisReply[]
            {
                "Name",
                "Steve"
            })
        };

        [Fact]
        public void TestBasicQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[-inf (33])",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicNegationQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => !(x.Age < 33)).ToList();
            _mock.Verify(x => x.Execute(
                    "FT.SEARCH",
                    "person-idx",
                    "-(@Age:[-inf (33])",
                    "LIMIT",
                    "0",
                    "100"));
        }

        [Fact]
        public void TestBasicQueryWithVariable()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < y).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[-inf (33])",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestFirstOrDefaultWithMixedLocals()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var heightList = new List<double> { 70.0, 68.0 };
            var y = 33;
            foreach (var height in heightList)
            {
                var collection = new RedisCollection<Person>(_mock.Object);
                var res = collection.FirstOrDefault(x => x.Age == y && x.Height == height);
                _mock.Verify(x => x.Execute(
                    "FT.SEARCH",
                    "person-idx",
                    $"((@Age:[33 33]) (@Height:[{height} {height}]))",
                    "LIMIT",
                    "0",
                    "1"));
            }
        }

        [Fact]
        public void TestBasicQueryWithExactNumericMatch()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age == y).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicFirstOrDefaultQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.FirstOrDefault(x => x.Age == y);
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public void TestBasicQueryNoNameIndex()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var y = 33;
            var collection = new RedisCollection<PersonNoName>(_mock.Object);
            var res = collection.FirstOrDefault(x => x.Age == y);
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "personnoname-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public void TestBasicOrQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33 || x.TagField == "Steve").ToList();
            Assert.Equal(32, res[0].Age);
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) | (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicOrQueryTwoTags()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.TagField == "Bob" || x.TagField == "Steve").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{Bob}) | (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100"));
            Assert.Equal(32, res[0].Age);
        }

        [Fact]
        public void TestBasicOrQueryWithNegation()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33 || x.TagField != "Steve" || x.Name != "Steve").ToList();
            Assert.Equal(32, res[0].Age);
            _mock.Verify(x => x.Execute(
                    "FT.SEARCH",
                    "person-idx",
                    "(((@Age:[-inf (33]) | (-@TagField:{Steve})) | (-@Name:\"Steve\"))",
                    "LIMIT",
                    "0",
                    "100"));
        }

        [Fact]
        public void TestBasicAndQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33 && x.TagField == "Steve").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100"));
            Assert.Equal(32, res[0].Age);
        }

        [Fact]
        public void TestBasicTagQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33 && x.TagField == "Steve").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) (@TagField:{Steve}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicThreeCluaseQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33 && x.TagField == "Steve" && x.Height >= 70).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(((@Age:[-inf (33]) (@TagField:{Steve})) (@Height:[70 inf]))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestGroupedThreeCluaseQuery()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Age < 33 && (x.TagField == "Steve" || x.Height >= 70)).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[-inf (33]) ((@TagField:{Steve}) | (@Height:[70 inf])))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryWithContains()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Name.Contains("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste)",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryWithStartsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Name.StartsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste*)",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestFuzzy()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Name.FuzzyMatch("Ste", 2)).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:%%Ste%%)",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestMatchStartsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Name.MatchStartsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste*)",
                "LIMIT",
                "0",
                "100"));
        }
        
        [Fact]
        public void TestMatchEndsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Name.MatchEndsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste)",
                "LIMIT",
                "0",
                "100"));
        }
        
        [Fact]
        public void TestMatchContains()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Name.MatchContains("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste*)",
                "LIMIT",
                "0",
                "100"));
        }
        
        [Fact]
        public void TestTagStartsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.TagField.StartsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{Ste*})",
                "LIMIT",
                "0",
                "100"));
        }
        
        [Fact]
        public void TestTagEndsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.TagField.EndsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{*Ste})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestTextEndsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Name.EndsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste)",
                "LIMIT",
                "0",
                "100"));
        }
        
        [Fact]
        public void TestTextStartsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Name.StartsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Ste*)",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryWithEndsWith()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Name.EndsWith("Ste")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:*Ste)",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryFromPropertyOfModel()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var modelObject = new Person() { Name = "Steve" };
            collection.Where(x => x.Name == modelObject.Name).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"Steve\")",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryFromPropertyOfModelWithStringInterpolation()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var modelObject = new Person() { Name = "Steve" };
            collection.Where(x => x.Name == $"A {nameof(Person)} named {modelObject.Name}").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"A Person named Steve\")",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryFromPropertyOfModelWithStringFormatFourArgs()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var modelObject = new Person() { Name = "Steve" };
            var a = "A";
            var named = "named";
            collection.Where(x => x.Name == string.Format("{0} {1} {2} {3}", a, nameof(Person), named, modelObject.Name)).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"A Person named Steve\")",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestBasicQueryWithContainsWithNegation()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => !x.Name.Contains("Ste")).ToList();
            Assert.Equal(32, res[0].Age);
            _mock.Verify(x => x.Execute(
                    "FT.SEARCH",
                    "person-idx",
                    "-(@Name:Ste)",
                    "LIMIT",
                    "0",
                    "100"));
        }

        [Fact]
        public void TestTwoPredicateQueryWithContains()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Name.Contains("Ste") || x.TagField == "John").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Name:Ste) | (@TagField:{John}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestTwoPredicateQueryWithPrefixMatching()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Name.Contains("Ste*") || x.TagField == "John").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Name:Ste*) | (@TagField:{John}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestGeoFilter()
        {
            _mock.Setup(x => x.Execute(
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
                ))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            Assert.Equal(32, res[0].Age);
        }

        [Fact]
        public void TestGeoFilterWithWhereClause()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.TagField == "Steve").GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            Assert.Equal(32, res[0].Age);
            _mock.Verify(x => x.Execute(
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
                ));
        }

        [Fact]
        public void TestSelect()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReplySelect);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Select(x => x.Name).ToList();
            _mock.Verify(x => x.Execute(
               "FT.SEARCH",
               "person-idx",
               "*",
               "LIMIT",
               "0",
               "100",
               "RETURN",
               "1",
               "Name"
               ));
        }

        [Fact]
        public void TestSelectComlexAnonType()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReplySelect);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Select(x => new { x.Name }).ToList();
            _mock.Verify(x => x.Execute(
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
               "Name"));
        }

        [Fact]
        public void TextEqualityExpression()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>())).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x => x.Name == "Steve").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"Steve\")",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestPaginationChunkSizesSinglePredicate()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>())).Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var res = collection.Where(x => x.Name == "Steve").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:\"Steve\")",
                "LIMIT",
                "0",
                "1000"));
        }

        [Fact]
        public void TestPaginationChunkSizesMultiplePredicates()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var res = collection.Where(x => x.TagField == "Steve").GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            Assert.Equal(32, res[0].Age);
            _mock.Verify(x => x.Execute(
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
            ));
        }

        [Fact]
        public void TestNestedObjectStringSearch()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var res = collection.Where(x => x.Address.City == "Newark").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_City:{Newark})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestNestedObjectStringSearchNested2Levels()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var res = collection.Where(x => x.Address.ForwardingAddress.City == "Newark").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_ForwardingAddress_City:{Newark})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestNestedObjectNumericSearch()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var res = collection.Where(x => x.Address.HouseNumber == 4).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_HouseNumber:[4 4])",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestNestedObjectNumericSearch2Levels()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var res = collection.Where(x => x.Address.ForwardingAddress.HouseNumber == 4).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Address_ForwardingAddress_HouseNumber:[4 4])",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestNestedQueryOfGeo()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            collection.GeoFilter(x => x.Address.Location, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            _mock.Verify(x => x.Execute(
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
            ));
        }

        [Fact]
        public void TestNestedQueryOfGeo2Levels()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            collection.GeoFilter(x => x.Address.ForwardingAddress.Location, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            _mock.Verify(x => x.Execute(
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
            ));
        }

        [Fact]
        public void TestArrayContains()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            collection.Where(x => x.NickNames.Contains("Steve")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestArrayContainsSpecialChar()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            collection.Where(x => x.NickNames.Contains("Steve@redis.com")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve\\@redis\\.com})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestArrayContainsVar()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            var steve = "Steve";
            collection.Where(x => x.NickNames.Contains(steve)).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void TestArrayContainsNested()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            collection.Where(x => x.Mother.NickNames.Contains("Di")).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Mother_NickNames:{Di})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public async Task TestUpdateJson()
        {
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            await collection.UpdateAsync(steve);
            _mock.Verify(x => x.ExecuteAsync("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33"));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonUnloadedScriptAsync()
        {
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).Throws(new RedisServerException("Failed on EVALSHA"));
            _mock.Setup(x => x.ExecuteAsync("EVAL", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            await collection.UpdateAsync(steve);
            _mock.Verify(x => x.ExecuteAsync("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33"));
            _mock.Verify(x => x.ExecuteAsync("EVAL", Scripts.JsonDiffResolution, "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33"));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public void TestUpdateJsonUnloadedScript()
        {
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.Execute("EVALSHA", It.IsAny<string[]>())).Throws(new RedisServerException("Failed on EVALSHA"));
            _mock.Setup(x => x.Execute("EVAL", It.IsAny<string[]>())).Returns("42");
            _mock.Setup(x => x.Execute("SCRIPT", It.IsAny<string[]>())).Returns("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            collection.Update(steve);
            _mock.Verify(x => x.Execute("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33"));
            _mock.Verify(x => x.Execute("EVAL", Scripts.JsonDiffResolution, "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33"));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonName()
        {
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("42");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Name = "Bob";
            await collection.UpdateAsync(steve);
            _mock.Verify(x => x.ExecuteAsync("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Name", "\"Bob\""));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonNestedObject()
        {
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("cbbf1c4fab5064f419e469cc51c563f8bf51e6fb");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Address = new Address { State = "Florida" };
            await collection.UpdateAsync(steve);
            var expected = $"{{{Environment.NewLine}  \"State\": \"Florida\"{Environment.NewLine}}}";
            _mock.Verify(x => x.ExecuteAsync("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Address", expected));

            steve.Address.City = "Satellite Beach";
            await collection.UpdateAsync(steve);
            expected = "\"Satellite Beach\"";
            _mock.Verify(x => x.ExecuteAsync("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Address.City", expected));

            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonWithDouble()
        {
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("42");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            steve.Height = 71.5;
            await collection.UpdateAsync(steve);
            _mock.Verify(x => x.ExecuteAsync("EVALSHA", It.IsAny<string>(), "1", "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET", "$.Age", "33", "SET", "$.Height", "71.5"));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestDeleteAsync()
        {
            const string key = "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N";
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("UNLINK", It.IsAny<string[]>())).ReturnsAsync("1");
            var colleciton = new RedisCollection<Person>(_mock.Object);
            var steve = colleciton.First(x => x.Name == "Steve");
            Assert.True(colleciton.StateManager.Data.ContainsKey(key));
            Assert.True(colleciton.StateManager.Snapshot.ContainsKey(key));
            await colleciton.DeleteAsync(steve);
            _mock.Verify(x => x.ExecuteAsync("UNLINK", key));
            Assert.False(colleciton.StateManager.Data.ContainsKey(key));
            Assert.False(colleciton.StateManager.Snapshot.ContainsKey(key));
        }

        [Fact]
        public void TestDelete()
        {
            const string key = "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N";
            _mock.Setup(x => x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("UNLINK", It.IsAny<string[]>())).ReturnsAsync("1");
            var colleciton = new RedisCollection<Person>(_mock.Object);
            var steve = colleciton.First(x => x.Name == "Steve");
            Assert.True(colleciton.StateManager.Data.ContainsKey(key));
            Assert.True(colleciton.StateManager.Snapshot.ContainsKey(key));
            colleciton.DeleteAsync(steve);
            _mock.Verify(x => x.ExecuteAsync("UNLINK", key));
            Assert.False(colleciton.StateManager.Data.ContainsKey(key));
            Assert.False(colleciton.StateManager.Snapshot.ContainsKey(key));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstAsync(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_mock.Object);
            if (useExpression)
            {
                _ = await collection.FirstAsync(x => x.TagField == "bob");
            }
            else
            {
                _ = await collection.FirstAsync();
            }

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstAsyncNone(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReplyNone);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_mock.Object);
            if (useExpression)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.FirstAsync(x => x.TagField == "bob"));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.FirstAsync());
            }
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstOrDefaultAsync(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_mock.Object);
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
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestFirstOrDefaultAsyncNone(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReplyNone);

            var collection = new RedisCollection<Person>(_mock.Object);

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
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleAsync(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var collection = new RedisCollection<Person>(_mock.Object);
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
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleAsyncNone(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReplyNone);

            var collection = new RedisCollection<Person>(_mock.Object);
            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            if (useExpression)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync(x => x.TagField == "bob"));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync());
            }

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleAsyncTwo(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);
            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            if (useExpression)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync(x => x.TagField == "bob"));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.SingleAsync());
            }

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleOrDefaultAsync(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

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
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleOrDefaultAsyncNone(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReplyNone);

            var collection = new RedisCollection<Person>(_mock.Object);

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
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestSingleOrDefaultAsyncTwo(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);

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
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAnyAsync(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";

            var collection = new RedisCollection<Person>(_mock.Object);

            var res = await (useExpression ? collection.AnyAsync(x => x.TagField == "bob") : collection.AnyAsync());
            Assert.True(res);
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAnyAsyncNone(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReplyNone);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var res = await (useExpression ? collection.AnyAsync(x => x.TagField == "bob") : collection.AnyAsync());

            Assert.False(res);
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestCountAsync(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var res = await (useExpression ? collection.CountAsync(x => x.TagField == "bob") : collection.CountAsync());
            Assert.Equal(1, res);
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestCount2Async(bool useExpression)
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);
            var expectedPredicate = useExpression ? "(@TagField:{bob})" : "*";
            var res = await (useExpression ? collection.CountAsync(x => x.TagField == "bob") : collection.CountAsync());
            Assert.Equal(2, res);
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0"));
        }

        [Fact]
        public async Task TestOrderByWithAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);
            var collection = new RedisCollection<Person>(_mock.Object);
            var expectedPredicate = "*";
            _ = await collection.OrderBy(x => x.Age).ToListAsync();
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "100",
                "SORTBY",
                "Age",
                "ASC"));
        }

        [Fact]
        public async Task TestOrderByDescendingWithAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);
            var collection = new RedisCollection<Person>(_mock.Object);
            var expectedPredicate = "*";
            _ = await collection.OrderByDescending(x => x.Age).ToListAsync();
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "100",
                "SORTBY",
                "Age",
                "DESC"));
        }

        [Fact]
        public async Task CombinedExpressionsWithFirstOrDefaultAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").FirstOrDefaultAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public async Task CombinedExpressionsWithFirstAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").FirstAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public async Task CombinedExpressionsWithAnyAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").AnyAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0"));
        }

        [Fact]
        public async Task CombinedExpressionsSingleOrDefaultAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").SingleOrDefaultAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public async Task CombinedExpressionsSingleAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").SingleAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public async Task CombinedExpressionsCountAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.Where(x => x.Name == "Bob").CountAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                expectedPredicate,
                "LIMIT",
                "0",
                "0"));
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionFirstOrDefaultAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply2Count);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).FirstOrDefaultAsync(x => x.Name == "Bob");

            _mock.Verify(x => x.ExecuteAsync(
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
                "mi"));
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionFirstAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).FirstAsync(x => x.Name == "Bob");

            _mock.Verify(x => x.ExecuteAsync(
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
                "mi"));
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionAnyAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).AnyAsync(x => x.Name == "Bob");

            _mock.Verify(x => x.ExecuteAsync(
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
                "mi"));
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionSingleAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).SingleAsync(x => x.Name == "Bob");

            _mock.Verify(x => x.ExecuteAsync(
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
                "mi"));
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionSingleOrDefaultAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).SingleOrDefaultAsync(x => x.Name == "Bob");

            _mock.Verify(x => x.ExecuteAsync(
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
                "mi"));
        }

        [Fact]
        public async Task TestCombinedExpressionWithExpressionCountAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            var expectedPredicate = "(@Name:\"Bob\")";
            _ = await collection.GeoFilter(x => x.Home, 5, 5, 10, GeoLocDistanceUnit.Miles).CountAsync(x => x.Name == "Bob");

            _mock.Verify(x => x.ExecuteAsync(
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
                "mi"));
        }

        [Fact]
        public async Task TestCreateIndexWithNoStopwords()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync("OK");

            await _mock.Object.CreateIndexAsync(typeof(ObjectWithZeroStopwords));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.CREATE",
                $"{nameof(ObjectWithZeroStopwords).ToLower()}-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ObjectWithZeroStopwords)}:",
                "STOPWORDS",
                "0",
                "SCHEMA", "Name", "TAG", "SEPARATOR", "|"));
        }

        [Fact]
        public async Task TestCreateIndexWithTwoStopwords()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync("OK");

            await _mock.Object.CreateIndexAsync(typeof(ObjectWithTwoStopwords));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.CREATE",
                $"{nameof(ObjectWithTwoStopwords).ToLower()}-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ObjectWithTwoStopwords)}:",
                "STOPWORDS", "2", "foo", "bar",
                "SCHEMA", "Name", "TAG", "SEPARATOR", "|"));
        }

        [Fact]
        public async Task TestCreateIndexWithStringlikeValueTypes()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync("OK");

            await _mock.Object.CreateIndexAsync(typeof(ObjectWithStringLikeValueTypes));

            _mock.Verify(x => x.ExecuteAsync("FT.CREATE",
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
                ));
        }

        [Fact]
        public async Task TestCreateIndexWithStringlikeValueTypesHash()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync("OK");

            await _mock.Object.CreateIndexAsync(typeof(ObjectWithStringLikeValueTypesHash));

            _mock.Verify(x => x.ExecuteAsync("FT.CREATE",
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
            ));
        }

        [Fact]
        public async Task TestCreateIndexWithDatetimeValue()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync("OK");

            await _mock.Object.CreateIndexAsync(typeof(ObjectWithDateTime));
            await _mock.Object.CreateIndexAsync(typeof(ObjectWithDateTimeHash));

            _mock.Verify(x => x.ExecuteAsync("FT.CREATE",
                "objectwithdatetime-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithDateTime:",
                "SCHEMA",
                "$.Timestamp", "AS", "Timestamp", "NUMERIC", "SORTABLE",
                "$.NullableTimestamp", "AS", "NullableTimestamp", "NUMERIC"
            ));

            _mock.Verify(x => x.ExecuteAsync("FT.CREATE",
                "objectwithdatetimehash-idx",
                "ON",
                "Hash",
                "PREFIX",
                "1",
                "Redis.OM.Unit.Tests.RediSearchTests.ObjectWithDateTimeHash:",
                "SCHEMA",
                "Timestamp", "NUMERIC",
                "NullableTimestamp", "NUMERIC"
            ));
        }

        [Fact]
        public async Task TestQueryOfUlid()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object);

            var ulid = Ulid.NewUlid();

            await collection.Where(x => x.Ulid == ulid).ToListAsync();
            var expectedPredicate = $"(@Ulid:{{{ulid}}})";

            _mock.Verify(x => x.ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100"));
        }

        [Fact]
        public async Task TestQueryOfGuid()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object);

            var guid = Guid.NewGuid();

            await collection.Where(x => x.Guid == guid).ToListAsync();

            var expectedPredicate = $"(@Guid:{{{ExpressionParserUtilities.EscapeTagField(guid.ToString())}}})";

            _mock.Verify(x => x.ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100"));
        }

        [Fact]
        public async Task TestQueryOfBoolean()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object);

            var boolean = true;

            await collection.Where(x => x.Boolean == true).ToListAsync();

            var expectedPredicate = $"(@Boolean:{{{true}}})";

            _mock.Verify(x => x.ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100"));
        }

        [Fact]
        public async Task TestQueryOfEnum()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object);

            var anEnum = AnEnum.two;

            await collection.Where(x => x.AnEnum == AnEnum.two && x.AnEnumAsInt == anEnum).ToListAsync();

            var expectedPredicate = $"((@AnEnum:{{{AnEnum.two}}}) (@AnEnumAsInt:[1 1]))";

            _mock.Verify(x => x.ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100"));
        }

        [Fact]
        public async Task TestQueryOfEnumHash()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypesHash>(_mock.Object);

            var anEnum = AnEnum.two;

            await collection.Where(x => x.AnEnum == AnEnum.two).ToListAsync();

            var expectedPredicate = $"(@AnEnum:[1 1])";

            _mock.Verify(x => x.ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypeshash-idx", expectedPredicate, "LIMIT", "0", "100"));
        }

        [Fact]
        public async Task TestGreaterThanEnumQuery()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object);

            var anEnum = AnEnum.two;

            await collection.Where(x => (int)x.AnEnumAsInt > 1).ToListAsync();

            var expectedPredicate = "(@AnEnumAsInt:[(1 inf])";

            _mock.Verify(x => x.ExecuteAsync("FT.SEARCH", "objectwithstringlikevaluetypes-idx", expectedPredicate, "LIMIT", "0", "100"));
        }

        [Fact]
        public async Task TestIndexCreationWithEmbeddedListOfDocuments()
        {
            _mock.Setup(x => x.ExecuteAsync("FT.CREATE", It.IsAny<string[]>())).ReturnsAsync("OK");
            await _mock.Object.CreateIndexAsync(typeof(ObjectWithEmbeddedArrayOfObjects));
            _mock.Verify(x => x.ExecuteAsync("FT.CREATE",
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
                "$.Name", "AS", "Name", "TAG", "SEPARATOR", "|", "$.Numeric", "AS", "Numeric", "NUMERIC"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjects()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach")).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "(@Addresses_City:{Satellite\\ Beach})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsEnum()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.Addresses.Any(a => a.AddressType == AddressType.Home)).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "(@Addresses_AddressType:{Home})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsExtraPredicate()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
               x.Numeric == 100 || x.Name == "Bob" && x.Addresses.Any(a => a.City == "Satellite Beach")).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Numeric:[100 100]) | ((@Name:{Bob}) (@Addresses_City:{Satellite\\ Beach})))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsMultipleAnys()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach") && x.AddressList.Any(x => x.City == "Newark")).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Addresses_City:{Satellite\\ Beach}) (@AddressList_City:{Newark}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsMultiplePredicatesInsideAny()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach" && a.State == "Florida")).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Addresses_City:{Satellite\\ Beach}) (@Addresses_State:{Florida}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsOtherTypes()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var boolean = true;
            var ulid = Ulid.NewUlid();
            var guid = Guid.NewGuid();

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.Addresses.Any(a => a.Ulid == ulid) && x.Addresses.Any(a => a.Guid == guid) && x.Addresses.Any(a => a.Boolean == boolean)).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                $"(((@Addresses_Ulid:{{{ulid}}}) (@Addresses_Guid:{{{ExpressionParserUtilities.EscapeTagField(guid.ToString())}}})) (@Addresses_Boolean:{{{boolean}}}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForListOfEmbeddedObjects()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.AddressList.Any(a => a.City == "Satellite Beach")).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "(@AddressList_City:{Satellite\\ Beach})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAnyQueryForArrayOfEmbeddedObjectsMultiVariat()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_mock.Object);

            await collection.Where(x =>
                x.Addresses.Any(a => a.City == "Satellite Beach" && a.State == "Florida")).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithembeddedarrayofobjects-idx",
                "((@Addresses_City:{Satellite\\ Beach}) (@Addresses_State:{Florida}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task SearchWithMultipleWhereClauses()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);

            await collection
                .Where(x => x.Name == "steve")
                .Where(x => x.Age == 32)
                .Where(x => x.TagField == "foo").ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "(((@Name:\"steve\") (@Age:[32 32])) (@TagField:{foo}))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public async Task TestAsyncMaterializationMethodsWithComibnedQueries()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object);
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

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{CountAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "0"));
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{AnyAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "0"));
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{SingleAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1"));
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{FirstAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1"));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{SingleOrDefaultAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1"));
            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{FirstOrDefaultAsync}) (@Age:[32 32]))",
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public void TestMaterializationMethodsWithComibnedQueries()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => x.Age == 32);
            _ = collection.Count(x => x.TagField == "Count");
            _ = collection.Any(x => x.TagField == "Any");
            _ = collection.Single(x => x.TagField == "Single");
            _ = collection.SingleOrDefault(x => x.TagField == "SingleOrDefault");
            _ = collection.First(x => x.TagField == "First");
            _ = collection.FirstOrDefault(x => x.TagField == "FirstOrDefault");

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{Count}))",
                "LIMIT",
                "0",
                "0"));
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{Any}))",
                "LIMIT",
                "0",
                "0"));
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{Single}))",
                "LIMIT",
                "0",
                "1"));
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{First}))",
                "LIMIT",
                "0",
                "1"));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{SingleOrDefault}))",
                "LIMIT",
                "0",
                "1"));
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@Age:[32 32]) (@TagField:{FirstOrDefault}))",
                "LIMIT",
                "0",
                "1"));
        }

        [Fact]
        public void SearchTagFieldContains()
        {
            var potentialTagFieldValues = new string[] { "Steve", "Alice", "Bob" };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => potentialTagFieldValues.Contains(x.TagField));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{Steve|Alice|Bob})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchTextFieldContains()
        {
            var potentialTextFieldValues = new string[] { "Steve", "Alice", "Bob" };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => potentialTextFieldValues.Contains(x.Name));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Name:Steve|Alice|Bob)",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchNumericFieldContains()
        {
            var potentialTagFieldValues = new int?[] { 35, 50, 60 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => potentialTagFieldValues.Contains(x.Age));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "@Age:[35 35]|@Age:[50 50]|@Age:[60 60]",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void Issue201()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var p1 = new Person() { Name = "Steve" };
            var collection = new RedisCollection<Person>(_mock.Object, 1000);
            collection.Where(x => x.NickNames.Contains(p1.Name)).ToList();

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNames:{Steve})",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void RangeOnDatetime()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var timestamp = DateTime.Now;
            var timeAnHourAgo = timestamp.Subtract(TimeSpan.FromHours(1));
            var timeAnHourAgoMilliseconds = new DateTimeOffset(timeAnHourAgo).ToUnixTimeMilliseconds();
            DateTime? timeTwoHoursAgoNullable = timestamp.Subtract(TimeSpan.FromHours(2));
            var timeTwoHoursAgoMilliseconds = new DateTimeOffset(timeTwoHoursAgoNullable.Value).ToUnixTimeMilliseconds();

            var collection = new RedisCollection<ObjectWithDateTime>(_mock.Object, 1000);

            var mockObj = new ObjectWithDateTime { Timestamp = timestamp.Subtract(TimeSpan.FromHours(3)) };
            var timeThreeHoursAgoMilliseconds = new DateTimeOffset(mockObj.Timestamp).ToUnixTimeMilliseconds();
            collection.Where(x => x.Timestamp == timeAnHourAgo).ToList();
            collection.Where(x => x.Timestamp > timeAnHourAgo).ToList();
            collection.Where(x => x.NullableTimestamp > timeAnHourAgo).ToList();

            collection.Where(x => x.Timestamp > timeTwoHoursAgoNullable).ToList();
            collection.Where(x => x.NullableTimestamp > timeTwoHoursAgoNullable).ToList();

            collection.Where(x => x.Timestamp > mockObj.Timestamp).ToList();
            collection.Where(x => x.NullableTimestamp > mockObj.Timestamp).ToList();

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public async Task RangeOnDatetimeAsync()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var timestamp = DateTime.Now;
            var timeAnHourAgo = timestamp.Subtract(TimeSpan.FromHours(1));
            DateTime? timeTwoHoursAgoNullable = timestamp.Subtract(TimeSpan.FromHours(2));
            var timeTwoHoursAgoMilliseconds = new DateTimeOffset(timeTwoHoursAgoNullable.Value).ToUnixTimeMilliseconds();
            var timeAnHourAgoMilliseconds = new DateTimeOffset(timeAnHourAgo).ToUnixTimeMilliseconds();
            var collection = new RedisCollection<ObjectWithDateTime>(_mock.Object, 1000);

            var mockObj = new ObjectWithDateTime { Timestamp = timestamp.Subtract(TimeSpan.FromHours(3)) };
            var timeThreeHoursAgoMilliseconds = new DateTimeOffset(mockObj.Timestamp).ToUnixTimeMilliseconds();
            await collection.Where(x => x.Timestamp == timeAnHourAgo).ToListAsync();
            await collection.Where(x => x.Timestamp > timeAnHourAgo).ToListAsync();
            await collection.Where(x => x.NullableTimestamp > timeAnHourAgo).ToListAsync();

            await collection.Where(x => x.Timestamp > timeTwoHoursAgoNullable).ToListAsync();
            await collection.Where(x => x.NullableTimestamp > timeTwoHoursAgoNullable).ToListAsync();

            await collection.Where(x => x.Timestamp > mockObj.Timestamp).ToListAsync();
            await collection.Where(x => x.NullableTimestamp > mockObj.Timestamp).ToListAsync();

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@Timestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetime-idx",
                $"(@NullableTimestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public async Task RangeOnDatetimeAsyncHash()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(_mockReply);

            var timestamp = DateTime.Now;
            var timeAnHourAgo = timestamp.Subtract(TimeSpan.FromHours(1));
            DateTime? timeTwoHoursAgoNullable = timestamp.Subtract(TimeSpan.FromHours(2));
            var timeTwoHoursAgoMilliseconds = new DateTimeOffset(timeTwoHoursAgoNullable.Value).ToUnixTimeMilliseconds();
            var timeAnHourAgoMilliseconds = new DateTimeOffset(timeAnHourAgo).ToUnixTimeMilliseconds();
            var collection = new RedisCollection<ObjectWithDateTimeHash>(_mock.Object, 1000);

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

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[{timeAnHourAgoMilliseconds} {timeAnHourAgoMilliseconds}])",
                "LIMIT",
                "0",
                "1000",
                "SORTBY",
                "Timestamp",
                "ASC"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@NullableTimestamp:[({timeAnHourAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@NullableTimestamp:[({timeTwoHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@Timestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.SEARCH",
                "objectwithdatetimehash-idx",
                $"(@NullableTimestamp:[({timeThreeHoursAgoMilliseconds} inf])",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public void SearchNumericFieldListContains()
        {
            var potentialTagFieldValues = new List<int?> { 35, 50, 60 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => potentialTagFieldValues.Contains(x.Age));
            collection.ToList();
            _mock.Verify(x => x.Execute(
               "FT.SEARCH",
               "person-idx",
               "@Age:[35 35]|@Age:[50 50]|@Age:[60 60]",
               "LIMIT",
               "0",
               "100"));
        }

        [Fact]
        public void SearchTagFieldAndTextListContains()
        {
            var potentialTagFieldValues = new List<string> { "Steve", "Alice", "Bob" };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => potentialTagFieldValues.Contains(x.TagField) || potentialTagFieldValues.Contains(x.Name));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{Steve|Alice|Bob}) | (@Name:Steve|Alice|Bob))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestNullResponseDoc()
        {
            int? nullVal = null;
            var nullResult = RedisResult.Create(nullVal);
            var res = new RedisReply[] { 1, $"foo:{Ulid.NewUlid()}", new(nullResult) };

            var query = new RedisQuery("fake-idx");
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(res);

            var result = _mock.Object.Search<Person>(query);
        }

        [Fact]
        public void SearchTagFieldAndTextListContainsWithEscapes()
        {
            var potentialTagFieldValues = new List<string> { "steve@example.com", "alice@example.com", "bob@example.com" };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).Where(x => potentialTagFieldValues.Contains(x.TagField) || potentialTagFieldValues.Contains(x.Name));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "((@TagField:{steve\\@example\\.com|alice\\@example\\.com|bob\\@example\\.com}) | (@Name:steve@example.com|alice@example.com|bob@example.com))",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchWithEmptyAny()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object);
            var any = collection.Any();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "0"));
            Assert.True(any);

            any = collection.Where(x => x.TagField == "foo").Any();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{foo})",
                "LIMIT",
                "0",
                "0"));

            Assert.True(any);
        }

        [Fact]
        public void TestContainsFromLocal()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = "steve";
            collection.Where(x => x.NickNamesList.Contains(steve)).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNamesList:{steve})",
                "LIMIT",
                "0",
                "100"));
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
            var potentialFieldValues = new Guid[] { guid1, guid2, guid3 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.Contains(x.Guid));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@Guid:{{{guid1Str}|{guid2Str}|{guid3Str}}})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestContainsFromProperty()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = new Person
            {
                Name = "steve"
            };
            collection.Where(x => x.NickNamesList.Contains(steve.Name)).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@NickNamesList:{steve})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchUlidFieldContains()
        {
            var ulid1 = Ulid.NewUlid();
            var ulid2 = Ulid.NewUlid();
            var ulid3 = Ulid.NewUlid();

            var potentialFieldValues = new Ulid[] { ulid1, ulid2, ulid3 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.Contains(x.Ulid));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@Ulid:{{{ulid1}|{ulid2}|{ulid3}}})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchEnumFieldContains()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new AnEnum[] { enum1, enum2, enum3 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.Contains(x.AnEnum));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@AnEnum:{{one|two|three}})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchNumericEnumFieldContains()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new AnEnum[] { enum1, enum2, enum3 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.Contains(x.AnEnumAsInt));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                "@AnEnumAsInt:[0 0]|@AnEnumAsInt:[1 1]|@AnEnumAsInt:[2 2]",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchEnumFieldContainsList()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new List<AnEnum> { enum1, enum2, enum3 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.Contains(x.AnEnum));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@AnEnum:{{one|two|three}})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchNumericEnumFieldContainsList()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new List<AnEnum> { enum1, enum2, enum3 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.Contains(x.AnEnumAsInt));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                "@AnEnumAsInt:[0 0]|@AnEnumAsInt:[1 1]|@AnEnumAsInt:[2 2]",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchEnumFieldContainsListAsProperty()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new { list = new List<AnEnum> { enum1, enum2, enum3 } };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.list.Contains(x.AnEnum));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                $"(@AnEnum:{{one|two|three}})",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void SearchNumericEnumFieldContainsListAsProperty()
        {
            var enum1 = AnEnum.one;
            var enum2 = AnEnum.two;
            var enum3 = AnEnum.three;

            var potentialFieldValues = new { list = new List<AnEnum> { enum1, enum2, enum3 } };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_mock.Object).Where(x => potentialFieldValues.list.Contains(x.AnEnumAsInt));
            collection.ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithstringlikevaluetypes-idx",
                "@AnEnumAsInt:[0 0]|@AnEnumAsInt:[1 1]|@AnEnumAsInt:[2 2]",
                "LIMIT",
                "0",
                "100"));
        }

        [Fact]
        public void TestNestedOrderBy()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object).OrderBy(x => x.Address.State).ToList();
            _mock.Verify(x => x.Execute("FT.SEARCH", "person-idx", "*", "LIMIT", "0", "100", "SORTBY", "Address_State", "ASC"));
        }

        [Fact]
        public void TestGeoFilterNested()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.GeoFilter(x => x.Address.Location, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            Assert.Equal(32, res[0].Age);
            _mock.Verify(x => x.Execute(
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
            ));
        }

        [Fact]
        public void TestSelectWithWhere()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<Person>(_mock.Object);
            _ = collection.Where(x => x.Age == 33).Select(x => x.Name).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@Age:[33 33])",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "Name"));
        }

        [Fact]
        public void TestNullableEnumQueries()

        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<ObjectWithNullableEnum>(_mock.Object);
            collection.Where(x => x.AnEnum == AnEnum.one && x.NullableStringEnum == AnEnum.two).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnullableenum-idx",
                "((@AnEnum:[0 0]) (@NullableStringEnum:{two}))",
                "LIMIT",
                "0",
                "100"
            ));
        }

        [Fact]
        public void TestEscapeForwardSlash()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            collection.Where(x => x.TagField == "a/test/string").ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "person-idx",
                "(@TagField:{a\\/test\\/string})",
                "LIMIT",
                "0",
                "100"
                ));
        }

        [Fact]
        public void TestMixedNestingIndexCreation()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns("OK");

            _mock.Object.CreateIndex(typeof(ComplexObjectWithCascadeAndJsonPath));

            _mock.Verify(x => x.Execute(
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
                "$.InnerJson.InnerInnerCascade.Arr[*]", "AS", "InnerJson_InnerInnerCascade_Arr", "TAG", "SEPARATOR", "|"));
        }

        [Fact]
        public void TestMixedNestingQuerying()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<ComplexObjectWithCascadeAndJsonPath>(_mock.Object);

            collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCascade.Tag == "hello");

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "complexobjectwithcascadeandjsonpath-idx",
                "(@InnerCascade_InnerInnerCascade_Tag:{hello})",
                "LIMIT",
                "0",
                "1"
            ));

            collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCascade.Num == 5);
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "complexobjectwithcascadeandjsonpath-idx",
                "(@InnerCascade_InnerInnerCascade_Num:[5 5])",
                "LIMIT",
                "0",
                "1"
            ));

            collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCollection.Any(x => x.Tag == "hello"));
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "complexobjectwithcascadeandjsonpath-idx",
                "(@InnerCascade_InnerInnerCollection_Tag:{hello})",
                "LIMIT",
                "0",
                "1"
            ));
        }
        
        [Fact]
        public async Task TestCreateIndexWithJsonPropertyName()
        {
            _mock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync("OK");

            await _mock.Object.CreateIndexAsync(typeof(ObjectWithPropertyNamesDefined));

            _mock.Verify(x => x.ExecuteAsync(
                "FT.CREATE",
                $"{nameof(ObjectWithPropertyNamesDefined).ToLower()}-idx",
                "ON",
                "Json",
                "PREFIX",
                "1",
                $"Redis.OM.Unit.Tests.{nameof(ObjectWithPropertyNamesDefined)}:",
                "SCHEMA", "$.notKey", "AS", "notKey", "TAG", "SEPARATOR", "|"));
        }

        [Fact]
        public void QueryNamedPropertiesJson()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithPropertyNamesDefined>(_mock.Object);

            collection.FirstOrDefault(x => x.Key == "hello");

            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                $"{nameof(ObjectWithPropertyNamesDefined).ToLower()}-idx",
                "(@notKey:{hello})",
                "LIMIT",
                "0",
                "1"
            ));
        }
        
        [Fact]
        public void TestMultipleContains()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithMultipleSearchableFields>(_mock.Object);
            Expression<Func<ObjectWithMultipleSearchableFields, bool>> whereExpressionFail = a => !a.FirstName.Contains("Andrey") && !a.LastName.Contains("Bred");

            collection.Where(whereExpressionFail).ToList();
            whereExpressionFail = a => !a.FirstName.Contains("Andrey") && a.LastName.Contains("Bred");
            collection.Where(whereExpressionFail).ToList();
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithmultiplesearchablefields-idx",
                "(-(@FirstName:Andrey) -(@LastName:Bred))",
                "LIMIT",
                "0",
                "100"
            ));
            
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithmultiplesearchablefields-idx",
                "(-(@FirstName:Andrey) (@LastName:Bred))",
                "LIMIT",
                "0",
                "100"
            ));
        }

        [Fact]
        public void TestSelectNestedObject()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            
            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Select(x => x.Address).ToList();
            res = collection.Select(x => x.Address.ForwardingAddress).ToList();
            
            _mock.Verify(x=>x.Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "$.Address"
                ));
            
            _mock.Verify(x=>x.Execute(
                "FT.SEARCH",
                "person-idx",
                "*",
                "LIMIT",
                "0",
                "100",
                "RETURN",
                "1",
                "$.Address.ForwardingAddress"
            ));
        }

        [Fact]
        public void NonNullableNumericFieldContains()
        {
            var ints = new int[] { 1, 2, 3 };
            var bytes = new byte[] { 4, 5, 6 };
            var sbytes = new sbyte[] { 7, 8, 9 };
            var shorts = new short[] { 10, 11, 12 };
            var uints = new uint[] { 13, 14, 15 };
            var longs = new long[] { 16, 17, 18 };
            var ulongs = new ulong[] { 19, 20, 21 };
            var doubles = new double[] { 22.5, 23, 24 };
            var floats = new float[] { 25.5F, 26, 27 };
            var ushorts = new ushort[] { 28, 29, 30 };
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);
            var collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => ints.Contains(x.Integer));
            collection.ToList();
            var expected = $"@{nameof(ObjectWithNumerics.Integer)}:[1 1]|@{nameof(ObjectWithNumerics.Integer)}:[2 2]|@{nameof(ObjectWithNumerics.Integer)}:[3 3]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => bytes.Contains(x.Byte));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Byte)}:[4 4]|@{nameof(ObjectWithNumerics.Byte)}:[5 5]|@{nameof(ObjectWithNumerics.Byte)}:[6 6]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => sbytes.Contains(x.SByte));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.SByte)}:[7 7]|@{nameof(ObjectWithNumerics.SByte)}:[8 8]|@{nameof(ObjectWithNumerics.SByte)}:[9 9]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => shorts.Contains(x.Short));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Short)}:[10 10]|@{nameof(ObjectWithNumerics.Short)}:[11 11]|@{nameof(ObjectWithNumerics.Short)}:[12 12]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => ushorts.Contains(x.UShort));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.UShort)}:[28 28]|@{nameof(ObjectWithNumerics.UShort)}:[29 29]|@{nameof(ObjectWithNumerics.UShort)}:[30 30]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => uints.Contains(x.UInt));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.UInt)}:[13 13]|@{nameof(ObjectWithNumerics.UInt)}:[14 14]|@{nameof(ObjectWithNumerics.UInt)}:[15 15]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => longs.Contains(x.Long));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Long)}:[16 16]|@{nameof(ObjectWithNumerics.Long)}:[17 17]|@{nameof(ObjectWithNumerics.Long)}:[18 18]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => ulongs.Contains(x.ULong));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.ULong)}:[19 19]|@{nameof(ObjectWithNumerics.ULong)}:[20 20]|@{nameof(ObjectWithNumerics.ULong)}:[21 21]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => doubles.Contains(x.Double));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Double)}:[22.5 22.5]|@{nameof(ObjectWithNumerics.Double)}:[23 23]|@{nameof(ObjectWithNumerics.Double)}:[24 24]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
            
            collection = new RedisCollection<ObjectWithNumerics>(_mock.Object).Where(x => floats.Contains(x.Float));
            collection.ToList();
            expected = $"@{nameof(ObjectWithNumerics.Float)}:[25.5 25.5]|@{nameof(ObjectWithNumerics.Float)}:[26 26]|@{nameof(ObjectWithNumerics.Float)}:[27 27]";
            _mock.Verify(x => x.Execute(
                "FT.SEARCH",
                "objectwithnumerics-idx",
                expected,
                "LIMIT",
                "0",
                "100"));
        }
    }
}