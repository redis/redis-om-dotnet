using NRedisPlus.RediSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NRedisPlus.Contracts;
using Xunit;

namespace NRedisPlus.Unit.Tests.RediSearchTests
{
    [Collection("Redis")]
    public class SearchFunctionalTests
    {

        public SearchFunctionalTests(RedisSetup setup)
        {
            _connection = setup.Connection;
            Setup().Wait();
            SetupHashModel().Wait();
        }
        private IRedisConnection _connection = null;

        private async Task SetupHashModel()
        {
            var names = new[] { "Steve", "Sarah", "Chris", "Theresa", "Frank", "Mary", "John", "Alice", "Bob" };
            var rand = new Random();
            var tasks = new List<Task>();
            for (var i = 0; i < 500; i++)
            {
                var person = new HashPerson()
                {
                    Name = names[rand.Next(0, names.Length)],
                    DepartmentNumber = rand.Next(1, 4),
                    Sales = rand.Next(50000, 1000000),
                    Age = rand.Next(22, 65),
                    Height = 58.0 + rand.NextDouble() * 15,
                    SalesAdjustment = rand.NextDouble()
                };

                tasks.Add(_connection.SetAsync(person));
            }
            await Task.WhenAll(tasks);
        }
        
        

        private async Task Setup()
        {            
            var names = new[] { "Steve", "Sarah", "Chris", "Theresa", "Frank", "Mary", "John", "Alice", "Bob" };
            var rand = new Random();
            var tasks = new List<Task>();
            for (var i = 0; i < 500; i++)
            {
                var person = new Person
                {
                    Name = names[rand.Next(0, names.Length)],
                    DepartmentNumber = rand.Next(1, 4),
                    Sales = rand.Next(50000, 1000000),
                    Age = rand.Next(22, 65),
                    Height = 58.0 + rand.NextDouble() * 15,
                    SalesAdjustment = rand.NextDouble()
                };

                tasks.Add(_connection.SetAsync(person));
            }
            await Task.WhenAll(tasks);
        }

        [Fact]
        public void EnumerateAllRecords()
        {
            var collection = new RedisCollection<Person>(_connection);
            var i = 0;
            
            foreach (var p in collection)
            {
                i++;
            }
            Assert.True(i >= 500);
            
        }

        [Fact]
        public void EnumerateAllRecordsAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            Task.Run(async () =>
            {
                var i = 0;
                await foreach (var p in collection)
                {
                    i++;
                }
                Assert.True(i >= 500);
            }).GetAwaiter().GetResult();            
        }

        [Fact]
        public void SelectAge()
        {
            var collection = new RedisCollection<Person>(_connection);
            var ages = collection.Select(x => x.Age).ToList();
            Assert.True(ages.Count >= 500);
        }

        [Fact]
        public void TestLimit()
        {
            var collection = new RedisCollection<Person>(_connection);
            var people = collection.Take(10);
            var i = 0;
            foreach (var person in people)
                i++;
            Assert.Equal(10, i);
        }

        [Fact]
        public void TestFirstOrDefault()
        {
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(new Person{Name = "John"});
            var firstJohn = collection.FirstOrDefault(x => x.Name == "John");
            Assert.NotNull(firstJohn);
            Assert.Equal("John",firstJohn.Name);
        }
        
        [Fact]
        public void TestAny()
        {
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(new Person{Name = "John"});
            var anyJohn = collection.Any(x => x.Name == "John");
            Assert.True(anyJohn);
        }

        [Fact]
        public void TestBasicQuerySpecialCharacters()
        {
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(new Person{Name = "Bond, James Bond", Email = "james.bond@sis.gov.uk", TagField = "James Bond"});
            var anyBond = collection.Any(x => x.Name == "Bond, James Bond" && x.Email == "james.bond@sis.gov.uk" && x.TagField == "James Bond");
            Assert.True(anyBond);
        }

        [Fact]
        public void TestSave()
        {
            var collection = new RedisCollection<Person>(_connection);
            var count = collection.Count();
            foreach (var person in collection)
            {
                person.Name = "Steve";
                person.Mother = new Person {Name = "Diane"};
            }
            
            collection.Save();
            var steves = collection.Where(x => x.Name == "Steve");
            Assert.Equal(count, steves.Count());
        }
        
        [Fact]
        public void TestSaveAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            Task.Run(async () =>
            {
                var chrises = collection.Where(x=>x.Name == "Chris");
                var count = chrises.Count();
                await foreach (var person in chrises)
                {
                    person.Name = "Augustine";
                    person.Mother = new Person {Name = "Monica"};
                }
                await collection.SaveAsync();
                var augustines = collection.Where(x => x.Name == "Augustine");
                var numSteves = augustines.Count();
                Assert.Equal(count, augustines.Count());
            }).GetAwaiter().GetResult();
        }
        
        [Fact]
        public void TestSaveHashAsync()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            Task.Run(async () =>
            {
                var count = collection.Count();
                await foreach (var person in collection)
                {
                    person.Name = "Steve";
                    person.Mother = new HashPerson {Name = "Diane"};
                }
                await collection.SaveAsync();
                var steves = collection.Where(x => x.Name == "Steve");
                Assert.Equal(count, steves.Count());
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public void TestSaveArray()
        {
            var maryNicknames = new List<string> {"Mary", "Mae", "Mimi", "Mitzi"};
            var maria = new Person {Name = "Maria", NickNames = maryNicknames};
            _connection.Set(maria);
            maryNicknames.RemoveAt(1);
            maryNicknames.RemoveAt(1);
            var collection = new RedisCollection<Person>(_connection);
            foreach (var mary in collection.Where(x => x.Name == "Maria"))
            {
                mary.NickNames = maryNicknames;
            }
            collection.Save();
            foreach (var mary in collection.Where(x => x.Name == "Maria"))
            {
                Assert.Equal(maryNicknames.ToArray(), mary.NickNames);
            }
            
        }
        
        [Fact]
        public void TestSaveArrayHash()
        {
            var maryNicknames = new List<string> {"Mary", "Mae", "Mimi", "Mitzi"};
            var maria = new HashPerson {Name = "Maria", NickNames = maryNicknames};
            _connection.Set(maria);
            maryNicknames.RemoveAt(1);
            maryNicknames.RemoveAt(1);
            var collection = new RedisCollection<HashPerson>(_connection);
            foreach (var mary in collection.Where(x => x.Name == "Maria"))
            {
                mary.NickNames = maryNicknames;
            }
            collection.Save();
            foreach (var mary in collection.Where(x => x.Name == "Maria"))
            {
                Assert.Equal(maryNicknames.ToArray(), mary.NickNames);
            }
            
        }
    }
}
