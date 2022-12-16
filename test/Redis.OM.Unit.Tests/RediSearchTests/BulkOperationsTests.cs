using Redis.OM.Contracts;
using Redis.OM.Searching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Collection("Redis")]
    public class BulkOperationsTests
    {
        public BulkOperationsTests(RedisSetup setup)
        {
            _connection = setup.Connection;
        }
        private IRedisConnection _connection = null;

        [Fact]
        public async Task TestBulkInsertAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var persons = new List<Person>() {
                new Person() { Name = "Alice", Age = 51, NickNames = new[] { "Ally", "Alie", "Al" }, },
                new Person() { Name = "Robert", Age = 37, NickNames = new[] { "Bobby", "Rob", "Bob" }, },
                new Person() { Name = "Jeeva", Age = 22, NickNames = new[] { "Jee", "Jeev", "J" }, },
                new Person() { Name = "Martin", Age = 60, NickNames = new[] { "Mart", "Mat", "tin" }, }
                };
            var keys = await collection.InsertAsync(persons);
            var people = collection.Where(x => x.NickNames.Contains("Bob") || x.NickNames.Contains("Alie")).ToList();
            Assert.Contains(people, x => x.Name == persons.First().Name);
        }

        [Fact]
        public void TestBulkInsertWithSameIds()
        {
            var collection = new RedisCollection<Person>(_connection);
            var persons = new List<Person>() {
                new Person() {Id="01GFZ9Y6CTEDHHXKT055N1YP3A" , Name = "Alice", Age = 51, NickNames = new[] { "Ally", "Alie", "Al" }, },
                new Person() {Id="01GFZ9Y6CTEDHHXKT055N1YP3A" , Name = "Robert", Age = 37, NickNames = new[] { "Bobby", "Rob", "Bob" }, },
                new Person() { Name = "Jeeva", Age = 22, NickNames = new[] { "Jee", "Jeev", "J" }, },
                new Person() { Name = "Martin", Age = 60, NickNames = new[] { "Mart", "Mat", "tin" }, }
                };
            collection.Insert(persons);
            var people = collection.Where(x => x.NickNames.Contains("Bob") || x.NickNames.Contains("Alie")).ToList();
            Assert.Equal(people.Count, persons.Count - 3);
            Assert.False(people.First().Name == persons.First().Name); // this fails because the Name field of people doesn't contains the Name value Alice
        }

        [Fact]
        public async Task BulkInsertAsync50Records()
        {
            var collection = new RedisCollection<Person>(_connection);

            var names = new[] { "Stever", "Martin", "Aegorn", "Robert", "Mary", "Joe", "Mark", "Otto" };
            var rand = new Random();
            var people = new List<Person>();
            for (var i = 0; i < 50; i++) // performance improment 1000 records in an avg of 200ms
            {
                people.Add(new Person
                {
                    Name = names[rand.Next(0, names.Length)],
                    DepartmentNumber = rand.Next(1, 4),
                    Sales = rand.Next(50000, 1000000),
                    Age = rand.Next(17, 21),
                    Height = 58.0 + rand.NextDouble() * 15,
                    SalesAdjustment = rand.NextDouble()
                }
                );
            }
            await collection.InsertAsync(people);
            var countPeople = collection.Where(x => x.Age >= 17 && x.Age <= 21).ToList().Count;
            Assert.Equal(people.Count, countPeople);
        }

        [Fact]
        public void TestBulkInsertHashesWithExpiration()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var PhineasFerb = new List<HashPerson>() {
                new HashPerson() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "#SummerVacation" },
                new HashPerson() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "#SummerVacation" }
            };

            collection.Insert(PhineasFerb, TimeSpan.FromMilliseconds(8000));
            var ttl = (long)_connection.Execute("PTTL", PhineasFerb[0].GetKey());
            Assert.True(ttl <= 8000);
            Assert.True(ttl >= 1000);
        }

        [Fact]
        public void TestBulkInsertWithExpiration()
        {
            var collection = new RedisCollection<Person>(_connection);
            var PhineasFerb = new List<Person>() {
                new Person() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "#SummerVacation" ,  NickNames = new[] { "Feb", "Fee" } },
                new Person() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "#SummerVacation",  NickNames = new[] { "Phineas", "Triangle Head", "Phine" } }
            };

            collection.Insert(PhineasFerb, TimeSpan.FromSeconds(8));
            var ttl = (long)_connection.Execute("PTTL", PhineasFerb[0].GetKey());
            Assert.True(ttl <= 8000);
            Assert.True(ttl >= 1000);
        }

        [Fact]
        public void TestBulk_InsertAndDel()
        {
            var collection = new RedisCollection<Person>(_connection);
            var PhineasFerbShow = new List<Person>() {
                new Person() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" , Address = new Address { State = "Tri-State Area"} },
                new Person() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation", Address = new Address { State = "Tri-State Area"} },
                new Person() { Name = "Dr.Doofenshmirtz", Age = 38, IsEngineer = true, TagField = "Villain", Address = new Address { State = "Tri-State Area"} },
                new Person() { Name = "Perry", Age = 5, IsEngineer = false, TagField = "Agent", Address = new Address { State = "Tri-State Area "} }
            };

            collection.Insert(PhineasFerbShow);
            var searchByState = collection.Where(x => x.Address.State == "Tri-State Area").ToList();
            collection.Delete(searchByState);
            var searchByTag = collection.FindById(searchByState[0].GetKey());
            Assert.Null(searchByTag);
        }

        [Fact]
        public async Task TestBulk_InsertAsyncAndDelAsyncForHashes()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var PhineasFerbShow = new List<HashPerson>() {
                new HashPerson() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" , Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation", Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Dr.Doofenshmirtz", Age = 38, IsEngineer = true, TagField = "Villain", Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Perry", Age = 5, IsEngineer = false, TagField = "Agent", Address = new Address { State = "Tri-State Area "} }
            };

            await collection.InsertAsync(PhineasFerbShow);
            var searchByName = await collection.Where(x => x.Name == "Dr.Doofenshmirtz" || x.Name == "Perry").ToListAsync();
            await collection.DeleteAsync(searchByName);
            var searchByTag = await collection.FindByIdAsync(searchByName[0].GetKey());
            Assert.Null(searchByTag);
        }
    }   
}
