using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class SearchTests
    {
        Mock<IRedisConnection> _mock = new Mock<IRedisConnection>();
        RedisReply _mockReply = new RedisReply[]
        {
            new RedisReply(1),
            new RedisReply("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new RedisReply(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            })
        };

        RedisReply _mockReplySelect = new RedisReply[]
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
            var heightList = new List<double> {70.0, 68.0};
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
            var res = collection.Where(x => x.Age < 33 || x.TagField != "Steve" || x.Name !="Steve").ToList();
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
            _mock.Setup(x => x.Execute(It.IsAny<string>(),It.IsAny<string[]>()))
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
            var res = collection.Where(x => x.Age < 33 && x.TagField == "Steve" && x.Height>=70).ToList();            
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
                "@Name:Ste",
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
            var modelObject = new Person() {Name = "Steve"};
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
            var modelObject = new Person() {Name = "Steve"};
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
            var modelObject = new Person() {Name = "Steve"};
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
                    "-@Name:Ste",
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
                "(@Name:Ste | (@TagField:{John}))",
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
                "(@Name:Ste* | (@TagField:{John}))",
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
            var res = collection.GeoFilter(x=>x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
            Assert.Equal(32, res[0].Age);
        }

        [Fact]
        public void TestGeoFilterWithWhereClause()
        {
            _mock.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(_mockReply);

            var collection = new RedisCollection<Person>(_mock.Object);
            var res = collection.Where(x=>x.TagField == "Steve").GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
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
            var res = collection.Select(x=>x.Name).ToList();
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
               "1",
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
            var res = collection.Where(x=>x.TagField == "Steve").GeoFilter(x => x.Home, 5, 6.7, 50, GeoLocDistanceUnit.Kilometers).ToList();
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
            var res = collection.Where(x=>x.Address.City == "Newark").ToList();
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
            var res = collection.Where(x=>x.Address.ForwardingAddress.City == "Newark").ToList();
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
            var res = collection.Where(x=>x.Address.HouseNumber == 4).ToList();
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
            var res = collection.Where(x=>x.Address.ForwardingAddress.HouseNumber == 4).ToList();
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
                "@NickNames:{Steve}",
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
                "@NickNames:{Steve}",
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
                "@Mother_NickNames:{Di}",
                "LIMIT",
                "0",
                "1000"
            ));
        }

        [Fact]
        public async Task TestUpdateJson()
        {
            _mock.Setup(x=>x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("42");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            await collection.Update(steve);
            _mock.Verify(x=>x.ExecuteAsync("EVALSHA","42","1","Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET","$.Age","33"));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonName()
        {
            _mock.Setup(x=>x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("42");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Name = "Bob";
            await collection.Update(steve);
            _mock.Verify(x=>x.ExecuteAsync("EVALSHA","42","1","Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET","$.Name","\"Bob\""));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonNestedObject()
        {
            _mock.Setup(x=>x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("42");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Address = new Address {State = "Florida"};
            await collection.Update(steve);
            var expected = $"{{{Environment.NewLine}  \"State\": \"Florida\"{Environment.NewLine}}}";
            _mock.Verify(x=>x.ExecuteAsync("EVALSHA","42","1","Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET","$.Address",expected));

            steve.Address.City = "Satellite Beach";
            await collection.Update(steve);
            expected = "\"Satellite Beach\"";
            _mock.Verify(x=>x.ExecuteAsync("EVALSHA","42","1","Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET","$.Address.City",expected));
            
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestUpdateJsonWithDouble()
        {
            _mock.Setup(x=>x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("EVALSHA", It.IsAny<string[]>())).ReturnsAsync("42");
            _mock.Setup(x => x.ExecuteAsync("SCRIPT", It.IsAny<string[]>())).ReturnsAsync("42");
            var collection = new RedisCollection<Person>(_mock.Object);
            var steve = collection.First(x => x.Name == "Steve");
            steve.Age = 33;
            steve.Height = 71.5;
            await collection.Update(steve);
            _mock.Verify(x=>x.ExecuteAsync("EVALSHA","42","1","Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N", "SET","$.Age","33", "SET","$.Height", "71.5"));
            Scripts.ShaCollection.Clear();
        }

        [Fact]
        public async Task TestDelete()
        {
            const string key = "Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N";
            _mock.Setup(x=>x.Execute("FT.SEARCH", It.IsAny<string[]>()))
                .Returns(_mockReply);
            _mock.Setup(x => x.ExecuteAsync("UNLINK", It.IsAny<string[]>())).ReturnsAsync("1");
            var colleciton = new RedisCollection<Person>(_mock.Object);
            var steve = colleciton.First(x => x.Name == "Steve");
            Assert.True(colleciton.StateManager.Data.ContainsKey(key));
            Assert.True(colleciton.StateManager.Snapshot.ContainsKey(key));
            await colleciton.Delete(steve);
            _mock.Verify(x=>x.ExecuteAsync("UNLINK",key));
            Assert.False(colleciton.StateManager.Data.ContainsKey(key));
            Assert.False(colleciton.StateManager.Snapshot.ContainsKey(key));
        }
    }
}