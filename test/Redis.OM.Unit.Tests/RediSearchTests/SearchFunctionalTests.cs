using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
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
        public async Task EnumerateAllRecordsAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var i = 0;
            await foreach (var p in collection)
            {
                i++;
            }
        }

        [Fact]
        public void SelectAge()
        {
            var collection = new RedisCollection<Person>(_connection);
            var ages = collection.Select(x => x.Age).ToList();
            foreach (var age in ages)
            {
                Assert.True(age >= 0 || age == null);               
            }
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
            var collection = new RedisCollection<Person>(_connection,10000);
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
        public async Task TestSaveAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var count = 0;
            await foreach (var person in collection.Where(x=>x.Name == "Chris"))
            {
                count++;
                person.Name = "Augustine";
                person.Mother = new Person {Name = "Monica"};
                person.IsEngineer = true;
            }
            await collection.SaveAsync();
            var augustines = collection.Where(x => x.Name == "Augustine");
            var numSteves = augustines.Count();
            Assert.Equal(count, augustines.Count());
        }

        [Fact]

        public async Task TestSaveAsyncSecondEnumeration()
        {
            var collection = new RedisCollection<Person>(_connection);
            var count = 0;
            await collection.Where(x => x.Name == "Chris").ToListAsync();
            await foreach (var person in collection.Where(x => x.Name == "Chris"))
            {
                count++;
                person.Name = "Thomas";
            }

            await collection.SaveAsync();
            var augustines = collection.Where(x => x.Name == "Thomas");
            Assert.Equal(count, augustines.Count());
            
        }

        [Fact]
        public async Task TestSaveHashAsync()
        {
            var collection = new RedisCollection<HashPerson>(_connection, 10000);
            var count = collection.Count();
            await foreach (var person in collection)
            {
                person.Name = "Steve";
                person.Mother = new HashPerson {Name = "Diane"};
            }
            await collection.SaveAsync();
            var steves = collection.Where(x => x.Name == "Steve");
            Assert.Equal(count, steves.Count());
        }

        [Fact]
        public void TestSaveArray()
        {
            try
            {
                var maryNicknames = new List<string> { "Mary", "Mae", "Mimi", "Mitzi" };
                var maria = new Person { Name = "Maria", NickNames = maryNicknames.ToArray() };
                _connection.Set(maria);
                maryNicknames.RemoveAt(1);
                maryNicknames.RemoveAt(1);
                var collection = new RedisCollection<Person>(_connection);
                foreach (var mary in collection.Where(x => x.Name == "Maria"))
                {
                    mary.NickNames = maryNicknames.ToArray();
                }
                collection.Save();
                foreach (var mary in collection.Where(x => x.Name == "Maria"))
                {
                    Assert.Equal(maryNicknames.ToArray(), mary.NickNames);
                }
            }
            catch(Exception)
            {
                throw;
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

        [Fact]
        public void TestSetGetWithLoc()
        {
            var testP = new Person {Name = "Steve", Home = new GeoLoc(1.0, 1.0)};
            var id =_connection.Set(testP);
            var reconstituded = _connection.Get<Person>(id);
            Assert.Equal("Steve", reconstituded.Name);
            Assert.Equal(new GeoLoc(1.0,1.0), reconstituded.Home);
        }

        [Fact]
        public void TestNestedObjectQuery()
        {
            var testP = new Person{Name = "Steve", Home = new GeoLoc(1.0, 1.0), Address = new Address{ City = "Newark"}};
            var id =_connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            Assert.True(collection.Where(x => x.Name == "Steve" && x.Address.City == "Newark").FirstOrDefault() != default);
        }
        
        [Fact]
        public void TestNestedObjectQuery2Levels()
        {
            var testP = new Person{Name = "Steve", Home = new GeoLoc(1.0, 1.0), Address = new Address{ ForwardingAddress = new Address{City = "Newark"}}};
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            Assert.True(collection.Where(x => x.Name == "Steve" && x.Address.ForwardingAddress.City == "Newark").FirstOrDefault() != default);
        }

        [Fact]
        public void TestArrayQuery()
        {
            var testP = new Person{Name = "Stephen", Home = new GeoLoc(1.0, 1.0), Address = new Address{ ForwardingAddress = new Address{City = "Newark"}}, NickNames = new []{"Steve"}};
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            var steve = collection.FirstOrDefault(x => x.NickNames.Contains("Steve"));
            Assert.Equal(id.Split(':')[1], steve.Id);
        }

        [Fact]
        public void TestArrayQuerySpecialChars()
        {
            var testP = new Person{Name = "Stephen", Home = new GeoLoc(1.0, 1.0), Address = new Address{ ForwardingAddress = new Address{City = "Newark"}}, NickNames = new []{"Steve@redis.com"}};
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            var steve = collection.FirstOrDefault(x => x.NickNames.Contains("Steve@redis.com"));
            Assert.Equal(id.Split(':')[1], steve.Id);
        }
        
        [Fact]
        public void TestListQuery()
        {
            var testP = new Person{Name = "Stephen", Home = new GeoLoc(1.0, 1.0), Address = new Address{ ForwardingAddress = new Address{City = "Newark"}}, NickNamesList = new List<string> {"Steve"}};
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            var steve = collection.FirstOrDefault(x => x.NickNamesList.Contains("Steve"));
            Assert.Equal(id.Split(':')[1], steve.Id);
        }
      
        [Fact]
        public void TestCountWithEmptyCollection()
        {
            var collection = new RedisCollection<ClassForEmptyRedisCollection>(_connection);
            var count = collection.Count();
            Assert.Equal(0,count);
        }
      
        [Fact]
        public void TestUpdate()
        {
            var collection = new RedisCollection<Person>(_connection);
            var testP = new Person {Name = "Steve", Age = 32};
            var key = collection.Insert(testP);
            var queriedP = collection.FindById(key);
            Assert.NotNull(queriedP);
            queriedP.Age = 33;
            collection.Update(queriedP);

            var secondQueriedP = collection.FindById(key);
            
            Assert.NotNull(secondQueriedP);
            Assert.Equal(33, secondQueriedP.Age);
            Assert.Equal(secondQueriedP.Id, queriedP.Id);
            Assert.Equal(testP.Id, secondQueriedP.Id);
        }
        
        [Fact]
        public async Task TestUpdateAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var testP = new Person {Name = "Steve", Age = 32};
            var key = await collection.InsertAsync(testP);
            var queriedP = await collection.FindByIdAsync(key);
            Assert.NotNull(queriedP);
            queriedP.Age = 33;
            await collection.UpdateAsync(queriedP);

            var secondQueriedP = await collection.FindByIdAsync(key);
            
            Assert.NotNull(secondQueriedP);
            Assert.Equal(33, secondQueriedP.Age);
            Assert.Equal(secondQueriedP.Id, queriedP.Id);
            Assert.Equal(testP.Id, secondQueriedP.Id);
        }

        [Fact]
        public async Task TestUpdateName()
        {
            var collection = new RedisCollection<Person>(_connection);
            var testP = new Person {Name = "Steve", Age = 32};
            var key = await collection.InsertAsync(testP);
            var id = testP.Id;
            var queriedP = collection.First(x => x.Id == id);
            Assert.NotNull(queriedP);
            queriedP.Name = "Bob";
            await collection.UpdateAsync(queriedP);

            var secondQueriedP = await collection.FindByIdAsync(key);
            
            Assert.NotNull(secondQueriedP);
            Assert.Equal("Bob", secondQueriedP.Name);
            Assert.Equal(secondQueriedP.Id, queriedP.Id);
            Assert.Equal(testP.Id, secondQueriedP.Id);
        }

        [Fact]
        public async Task TestUpdateHashPerson()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var testP = new HashPerson {Name = "Steve", Age = 32};
            var key = await collection.InsertAsync(testP);
            var queriedP = await collection.FindByIdAsync(key);
            Assert.NotNull(queriedP);
            queriedP.Age = 33;
            await collection.UpdateAsync(queriedP);

            var secondQueriedP = await collection.FindByIdAsync(key);
            
            Assert.NotNull(secondQueriedP);
            Assert.Equal(33, secondQueriedP.Age);
            Assert.Equal(secondQueriedP.Id, queriedP.Id);
            Assert.Equal(testP.Id, secondQueriedP.Id);
        }

        [Fact]
        public async Task TestToListAsync()
        {
            var collection = new RedisCollection<Person>(_connection,10000);
            var list = await collection.ToListAsync();
            
            Assert.Equal(collection.Count(), list.Count);
        }

        [Fact]
        public async Task CountAsync()
        {
            var collection = new RedisCollection<Person>(_connection, 10000);
            var count = await collection.CountAsync();
            Assert.Equal(collection.Count(), count);
        }

        [Fact]
        public async Task TestFirstOrDefaultAsync()
        {
            var collection = new RedisCollection<Person>(_connection,10000);
            Assert.NotNull(await collection.FirstOrDefaultAsync());
        }

        [Fact]
        public async Task TestAnyAsync()
        {
            var collection = new RedisCollection<Person>(_connection,10000);
            Assert.True(await collection.AnyAsync());
        }

        [Fact]
        public async Task TestSingleAsync()
        {
            var person = new Person {Name = "foo"};
            var collection = new RedisCollection<Person>(_connection,10000);
            await collection.InsertAsync(person);
            var id = person.Id;
            var res = await collection.SingleAsync(x => x.Id == id);
            Assert.Equal("foo",res.Name);
        }
        
        
        [Fact]
        public async Task TestNonExistentPersonJson()
        {
            var collection = new RedisCollection<Person>(_connection);
            var result = await collection.FindByIdAsync("NotARealId");
            Assert.Null(result);
        }
        
        [Fact]
        public async Task TestNonExistentHashPerson()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var result = await collection.FindByIdAsync("NotARealId");
            Assert.Null(result);
        }
        
        [Fact]
        public async Task TestNonExistentPersonJsonGet()
        {
            var result = await _connection.GetAsync<Person>("NotARealId");
            Assert.Null(result);
        }
        
        [Fact]
        public async Task TestNonExistentHashPersonGet()
        {
            var result = await _connection.GetAsync<HashPerson>("NotARealId");
            Assert.Null(result);
        }

        [Fact]
        public async Task FindByIdAsync()
        {
            var person = new Person {Name = "Bob"};
            var collection = new RedisCollection<Person>(_connection);
            await collection.InsertAsync(person);
            var alsoBob = await collection.FindByIdAsync(person.Id);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob",person.Name);
        }

        [Fact]
        public void FindById()
        {
            var person = new Person {Name = "Bob"};
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(person);
            var alsoBob = collection.FindById(person.Id);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob",person.Name);
        }

        [Fact]
        public void FindByKey()
        {
            var person = new Person {Name = "Bob"};
            var collection = new RedisCollection<Person>(_connection);
            var key = collection.Insert(person);
            var alsoBob = collection.FindById(key);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob",person.Name);
        }

        [Fact]
        public async Task FindByKeyAsync()
        {
            var person = new Person {Name = "Bob"};
            var collection = new RedisCollection<Person>(_connection);
            var key = await collection.InsertAsync(person);
            var alsoBob = await collection.FindByIdAsync(key);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob",person.Name);
        }

        [Fact]
        public async Task SearchByUlid()
        {
            var ulid = Ulid.NewUlid();
            var obj = new ObjectWithStringLikeValueTypes
            {
                Ulid = ulid
            };
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var key = await collection.InsertAsync(obj);
            var alsoObj = await collection.FirstOrDefaultAsync(x => x.Ulid == ulid);
            Assert.NotNull(alsoObj);
        }
        
        [Fact]
        public async Task SearchByBoolean()
        {
            var obj = new ObjectWithStringLikeValueTypes
            {
                Boolean = true
            };
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var key = await collection.InsertAsync(obj);
            var alsoObj = await collection.FirstOrDefaultAsync(x => x.Boolean == true);
            Assert.NotNull(alsoObj);
            alsoObj = await collection.FirstOrDefaultAsync(x => x.Boolean);
            Assert.NotNull(alsoObj);
        }
        
        [Fact]
        public async Task SearchByBooleanFalse()
        {
            var obj = new ObjectWithStringLikeValueTypes
            {
                Boolean = false
            };
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var key = await collection.InsertAsync(obj);
            var alsoObj = await collection.FirstOrDefaultAsync(x => x.Boolean == false);
            Assert.NotNull(alsoObj);
            alsoObj = await collection.FirstOrDefaultAsync(x => !x.Boolean);
            Assert.NotNull(alsoObj);
        }

        [Fact]
        public async Task TestSearchByStringEnum()
        {
            var obj = new ObjectWithStringLikeValueTypes() {AnEnum = AnEnum.two, AnEnumAsInt = AnEnum.three};
            await _connection.SetAsync(obj);
            var anEnum = AnEnum.two;
            var three = AnEnum.three;
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var result = await collection.Where(x => x.AnEnum == AnEnum.two).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => x.AnEnum == anEnum).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => x.AnEnum == obj.AnEnum).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => (int)x.AnEnumAsInt > 1).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => x.AnEnumAsInt > AnEnum.two).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => x.AnEnumAsInt == AnEnum.three).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => x.AnEnumAsInt == three).ToListAsync();
            Assert.NotEmpty(result);
            result = await collection.Where(x => x.AnEnumAsInt == obj.AnEnumAsInt).ToListAsync();
            Assert.NotEmpty(result);
        }
        
        [Fact]
        public async Task TestSearchByStringEnumHash()
        {
            var obj = new ObjectWithStringLikeValueTypesHash() {AnEnum = AnEnum.two};
            await _connection.SetAsync(obj);
            var anEnum = AnEnum.two;
            var collection = new RedisCollection<ObjectWithStringLikeValueTypesHash>(_connection);
            var result = await collection.Where(x => x.AnEnum == AnEnum.two).ToListAsync();
            Assert.NotEmpty(result);
            Assert.Equal(AnEnum.two, result.First().AnEnum);
            result = await collection.Where(x => x.AnEnum == anEnum).ToListAsync();
            Assert.NotEmpty(result);
            Assert.Equal(AnEnum.two, result.First().AnEnum);
            result = await collection.Where(x => x.AnEnum == obj.AnEnum).ToListAsync();
            Assert.NotEmpty(result);
            Assert.Equal(AnEnum.two, result.First().AnEnum);
        }

        [Fact]
        public async Task TestAnySearchEmbeddedObjects()
        {
            var obj = new ObjectWithEmbeddedArrayOfObjects()
            {
                Name = "Bob",
                Numeric = 100,
                Addresses = new[] {new Address {City = "Newark", State = "New Jersey"}},
                AddressList = new List<Address> {new() {City = "Satellite Beach", State = "Florida"}}
            };

            await _connection.SetAsync(obj);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_connection);

            var results = await collection.Where(x => x.Addresses.Any(x => x.City == "Newark")).ToListAsync();
            Assert.NotEmpty(results);
            results = await collection.Where(x => x.AddressList.Any(x => x.City == "Satellite Beach")).ToListAsync();
            Assert.NotEmpty(results);
            
        }
    }
}
