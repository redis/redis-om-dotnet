using System;
using System.Linq;
using Redis.OM.Modeling;
using Xunit;

namespace Redis.OM.Unit.Tests.SearchJsonTests
{
    public class RedisJsonIndexTests
    {
        
        [Document(IndexName = "person-idx", StorageType = StorageType.Json)]
        public class Person
        {
            [Searchable(Sortable = true)]
            public string Name { get; set; }
            [Indexed]
            public string Tag { get; set; }
            [Indexed(Sortable = true)]
            public int Age { get; set; }
            [Indexed(Sortable = true)]
            public double Height { get; set; }
            public string[] NickNames { get; set; }
        }
        
        [Document(IndexName = "person-idx", StorageType = StorageType.Json)]
        public class PersonWithIndexedNickNames
        {
            [Searchable(Sortable = true)]
            public string Name { get; set; }
            [Indexed]
            public string Tag { get; set; }
            [Indexed(Sortable = true)]
            public int Age { get; set; }
            [Indexed(Sortable = true)]
            public double Height { get; set; }
            [Indexed]
            public string[] NickNames { get; set; }
        }

        [Document(IndexName = "person-idx", StorageType = StorageType.Json)]
        public class NestedPerson
        {
            [Searchable(Sortable = true)]
            public string Name { get; set; }
            
            [Indexed]
            public string Tag { get; set; }
            
            [Indexed(Sortable = true)]
            public int Age { get; set; }
            
            [Indexed(Sortable = true)]
            public double Height { get; set; }
            
            public string[] NickNames { get; set; }
            
            [Indexed(JsonPath = "$.ZipCode")]
            [Indexed(JsonPath = "$.City")]
            [Searchable(JsonPath = "$.StreetName", Aggregatable = true)]
            public Address Address { get; set; }
        
            [Indexed(CascadeDepth = 1)]
            public Address WorkAddress { get; set; }
        }
        
        [Document(IndexName = "person-idx", StorageType = StorageType.Json)]
        public class NestedPersonCascade2
        {
            [Searchable(Sortable = true)]
            public string Name { get; set; }
            
            [Indexed]
            public string Tag { get; set; }
            
            [Indexed(Sortable = true)]
            public int Age { get; set; }
            
            [Indexed(Sortable = true)]
            public double Height { get; set; }
            
            public string[] NickNames { get; set; }
            
            [Indexed(JsonPath = "$.ZipCode")]
            [Indexed(JsonPath = "$.City")]
            [Searchable(JsonPath = "$.StreetName", Aggregatable = true)]
            public Address Address { get; set; }
        
            [Indexed(CascadeDepth = 2)]
            public Address WorkAddress { get; set; }
        }

        
        private void Setup() 
        {
            var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            var conf = new RedisConnectionConfiguration();
            conf.Host = host;
            var connection = conf.Connect();
            connection.DropIndexAndAssociatedRecords(typeof(NestedPerson));
            connection.CreateIndex(typeof(NestedPerson));
        }        

        [Fact]
        public void TestIndexSerializationHappyPath()
        {
            var expected = new[] { "person-idx",
                "ON", "Json", "PREFIX", "1", "Redis.OM.Unit.Tests.SearchJsonTests.RedisJsonIndexTests+Person:", "SCHEMA",
                "$.Name", "AS", "Name", "TEXT", "SORTABLE", "$.Tag", "AS","Tag","TAG", "SEPARATOR", "|","$.Age", "AS", "Age", 
                "NUMERIC","SORTABLE", "$.Height", "AS", "Height", "NUMERIC", "SORTABLE" };
            var indexArr = typeof(Person).SerializeIndex();

            for(var i = 0; i < indexArr.Length; i++)
            {
                Assert.Equal(expected[i], indexArr[i]);
            }
            
        }
        
        [Fact]
        public void TestIndexSerializationNestedObject()
        {
            var expected = new[] { "person-idx",
                "ON", "Json", "PREFIX", "1", "Redis.OM.Unit.Tests.SearchJsonTests.RedisJsonIndexTests+NestedPerson:", "SCHEMA",
                "$.Name", "AS", "Name", "TEXT", "SORTABLE", 
                "$.Tag", "AS","Tag","TAG", "SEPARATOR", "|",
                "$.Age", "AS", "Age", "NUMERIC","SORTABLE", 
                "$.Height", "AS", "Height", "NUMERIC", "SORTABLE", 
                "$.Address.ZipCode", "AS", "Address_ZipCode", "TAG", "SEPARATOR", "|",
                "$.Address.City", "AS", "Address_City", "TAG", "SEPARATOR", "|", 
                "$.Address.StreetName", "AS", "Address_StreetName", "TEXT", "SORTABLE",
                "$.WorkAddress.City", "AS","WorkAddress_City", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.AddressType", "AS", "WorkAddress_AddressType", "TAG",
                "$.WorkAddress.State", "AS", "WorkAddress_State", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.Location", "AS", "WorkAddress_Location", "GEO", 
                "$.WorkAddress.HouseNumber", "AS", "WorkAddress_HouseNumber", "NUMERIC",
                "$.WorkAddress.Boolean", "AS", "WorkAddress_Boolean", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.Ulid", "AS", "WorkAddress_Ulid", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.Guid", "AS", "WorkAddress_Guid", "TAG", "SEPARATOR", "|",
                
            };
            var indexArr = typeof(NestedPerson).SerializeIndex();
            Assert.Equal(expected.OrderBy(x=>x), indexArr.OrderBy(x=>x));
        }
        
        [Fact]
        public void TestIndexSerializationNestedObjectCascade2()
        {
            var expected = new[] { "person-idx",
                "ON", "Json", "PREFIX", "1", "Redis.OM.Unit.Tests.SearchJsonTests.RedisJsonIndexTests+NestedPersonCascade2:", "SCHEMA",
                "$.Name", "AS", "Name", "TEXT", "SORTABLE", 
                "$.Tag", "AS","Tag","TAG", "SEPARATOR", "|",
                "$.Age", "AS", "Age", "NUMERIC","SORTABLE", 
                "$.Height", "AS", "Height", "NUMERIC", "SORTABLE", 
                "$.Address.ZipCode", "AS", "Address_ZipCode", "TAG", "SEPARATOR", "|", 
                "$.Address.City", "AS", "Address_City", "TAG", "SEPARATOR", "|", 
                "$.Address.StreetName", "AS", "Address_StreetName", "TEXT", "SORTABLE",
                "$.WorkAddress.City", "AS","WorkAddress_City", "TAG", "SEPARATOR", "|",  
                "$.WorkAddress.AddressType", "AS", "WorkAddress_AddressType", "TAG",
                "$.WorkAddress.State", "AS", "WorkAddress_State", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.ForwardingAddress.City", "AS", "WorkAddress_ForwardingAddress_City", "TAG", "SEPARATOR", "|", 
                "$.WorkAddress.ForwardingAddress.AddressType", "AS", "WorkAddress_ForwardingAddress_AddressType", "TAG",
                "$.WorkAddress.ForwardingAddress.State", "AS", "WorkAddress_ForwardingAddress_State", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.ForwardingAddress.Location", "AS", "WorkAddress_ForwardingAddress_Location", "GEO",
                "$.WorkAddress.ForwardingAddress.HouseNumber", "AS", "WorkAddress_ForwardingAddress_HouseNumber", "NUMERIC",
                "$.WorkAddress.ForwardingAddress.Boolean", "AS", "WorkAddress_ForwardingAddress_Boolean", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.ForwardingAddress.Ulid", "AS", "WorkAddress_ForwardingAddress_Ulid", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.ForwardingAddress.Guid", "AS", "WorkAddress_ForwardingAddress_Guid", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.Location", "AS", "WorkAddress_Location", "GEO", 
                "$.WorkAddress.HouseNumber", "AS", "WorkAddress_HouseNumber", "NUMERIC",
                "$.WorkAddress.Boolean", "AS", "WorkAddress_Boolean", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.Ulid", "AS", "WorkAddress_Ulid", "TAG", "SEPARATOR", "|",
                "$.WorkAddress.Guid", "AS", "WorkAddress_Guid", "TAG", "SEPARATOR", "|",
                
            };
            var indexArr = typeof(NestedPersonCascade2).SerializeIndex();
            Assert.Equal(expected.OrderBy(x=>x), indexArr.OrderBy(x=>x));
        }

        [Fact]
        public void TestIndexSerializationWithNickNames()
        {
            var expected = new[] { "person-idx",
                "ON", "Json", "PREFIX", "1", "Redis.OM.Unit.Tests.SearchJsonTests.RedisJsonIndexTests+PersonWithIndexedNickNames:", "SCHEMA",
                "$.Name", "AS", "Name", "TEXT", "SORTABLE", 
                "$.Tag", "AS","Tag","TAG", "SEPARATOR", "|",
                "$.Age", "AS", "Age", "NUMERIC","SORTABLE", 
                "$.Height", "AS", "Height", "NUMERIC", "SORTABLE",
                "$.NickNames[*]", "AS","NickNames","TAG",
                
            };
            var indexArr = typeof(PersonWithIndexedNickNames).SerializeIndex();

            for(var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], indexArr[i]);
            }
        }
        
    }
}
