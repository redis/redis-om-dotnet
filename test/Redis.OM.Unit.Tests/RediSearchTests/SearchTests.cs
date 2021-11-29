using Moq;
using Moq.Language.Flow;
using System;
using System.Linq;
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
            new RedisReply("Person:33b58265-2656-4c5e-8476-7246549797d1"),
            new RedisReply(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71}"
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
    }
}