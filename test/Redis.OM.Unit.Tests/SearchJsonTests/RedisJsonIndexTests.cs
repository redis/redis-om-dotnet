using System;
using System.Linq;
using Redis.OM;
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

        
        private void Setup() 
        {
            var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            var conf = new RedisConnectionConfiguration();
            conf.Host = host;
            var connection = conf.Connect();
            connection.DropIndexAndAssociatedRecords(typeof(Person));
            connection.CreateIndex(typeof(Person));
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
    }
}
