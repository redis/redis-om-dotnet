using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Unit.Tests.RediSearchTests;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Redis.OM.Unit.Tests.SearchJsonTests
{
    [Collection("Redis")]
    public class RedisJsonNestedComposeValueTest
    {
        public RedisJsonNestedComposeValueTest(RedisSetup setup)
        {
            _connection = setup.Connection;
        }
        private IRedisConnection _connection = null;

        [Document(StorageType = StorageType.Json)]
        public partial class PersonWithNestedArrayOfObject
        {
            [RedisIdField]
            [Indexed]
            public string Id { get; set; }
            [Searchable]
            public string Name { get; set; }
            [Indexed(JsonPath = "$.City")]
            [Indexed(JsonPath = "$.State")]
            public List<Address> Addresses { get; set; }
        }

        [Fact]
        public void TestEmbeddedMultipleList()
        {
            var currentAddress = new Address { State = "TN", City = "Chennai" };
            var permanentAddress = new Address { State = "TN", City = "Attur" };
            var personWithDualAddress = new PersonWithNestedArrayOfObject { Name = "Jeeva", Addresses = new List<Address> { currentAddress, permanentAddress } };
            var collection = new RedisCollection<PersonWithNestedArrayOfObject>(_connection);
            collection.Insert(personWithDualAddress);
            var person = collection.FindById(personWithDualAddress.Id);
            person.Addresses[0].State = person.Addresses[0].State + "(Tamil Nadu)";
            collection.Save();
            var result = collection.Where(x => x.Addresses.Any(x => x.State == "TN(Tamil Nadu)")).ToList();
            Assert.NotEmpty(result);
            collection.Delete(person);
        }

        [Fact]
        public async Task TestAnySearchEmbeddedObjects()
        {
            var obj = new ObjectWithEmbeddedArrayOfObjects()
            {
                Name = "John",
                Numeric = 100,
                Addresses = new[] { new Address { City = "Newark", State = "New Jersey" } },
                AddressList = new List<Address> { new() { City = "Satellite Beach", State = "Florida" } }
            };

            await _connection.SetAsync(obj);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_connection);

            var results = await collection.Where(x => x.Addresses.Any(x => x.State == "New Jersey")).ToListAsync();
            Assert.NotEmpty(results);
            results[0].Addresses[0].City = "Satellite Beach";
            results[0].Addresses[0].State = "Florida";
            await collection.SaveAsync();
            results = await collection.Where(x => x.Addresses.Any(x => x.City == "Satellite Beach")).ToListAsync();
            Assert.NotEmpty(results);
            collection.Delete(obj);
        }

        [Fact]
        public async Task TestEmbeddedMultipleListWithUpdate()
        {
            var currentAddress = new Address { State = "KL", City = "Kozhikode" };
            var permanentAddress = new Address { State = "TN", City = "Salem" };
            var personWithDualAddress = new PersonWithNestedArrayOfObject { Name = "Raja", Addresses = new List<Address> { currentAddress, permanentAddress } };
            var collection = new RedisCollection<PersonWithNestedArrayOfObject>(_connection);
            collection.Insert(personWithDualAddress);
            var person = collection.FindById(personWithDualAddress.Id);
            person.Addresses[0].State = person.Addresses[0].State + "(Kerala)";
            await  collection.UpdateAsync(person);
            var result = collection.Where(x => x.Addresses.Any(x => x.State == "KL(Kerala)")).ToList();
            Assert.NotEmpty(result);
            collection.Delete(person);
        }
        
        [Fact]
        public void TestEmbeddedMultipleListWithUpdateInsert()
        {
            var currentAddress = new Address { State = "KL", City = "Kozhikode" };
            var permanentAddress = new Address { State = "TN", City = "Salem" };
            var addressToBeInserted = new Address { State = "FL", City = "Hollywood" };
            var personWithDualAddress = new PersonWithNestedArrayOfObject { Name = "Raja", Addresses = new List<Address> { currentAddress, permanentAddress } };
            var collection = new RedisCollection<PersonWithNestedArrayOfObject>(_connection);
            collection.Insert(personWithDualAddress);
            var person = collection.FindById(personWithDualAddress.Id);
            person.Addresses.Add(addressToBeInserted);
            collection.Update(person);
            var result = collection.Where(x => x.Addresses.Any(x => x.City == "Hollywood")).ToList();
            Assert.NotEmpty(result);
            collection.Delete(person);
        }
    }
}
