using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
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
            for (var i = 0; i < 50; i++)
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
            for (var i = 0; i < 50; i++)
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
            collection.Insert(new Person { Name = "John" });
            var firstJohn = collection.FirstOrDefault(x => x.Name == "John");
            Assert.NotNull(firstJohn);
            Assert.Equal("John", firstJohn.Name);
        }

        [Fact]
        public void TestAny()
        {
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(new Person { Name = "John" });
            var anyJohn = collection.Any(x => x.Name == "John");
            Assert.True(anyJohn);
        }

        [Fact]
        public void TestBasicQuerySpecialCharacters()
        {
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(new Person { Name = "Bond, James Bond", Email = "james.bond@sis.gov.uk", TagField = "James Bond" });
            var anyBond = collection.Any(x => x.Name == "Bond, James Bond" && x.Email == "james.bond@sis.gov.uk" && x.TagField == "James Bond");
            Assert.True(anyBond);
        }

        [Fact]
        public void TestSave()
        {
            var collection = new RedisCollection<BasicJsonObjectTestSave>(_connection, 10000);
            
            for(var i = 0; i < 10; i++)
            {
                collection.Insert(new BasicJsonObjectTestSave() { Name = "TestSaveBefore" });
            }
            var count = 0;
            foreach (var person in collection)
            {
                count++;
                person.Name = "TestSave";
            }

            collection.Save();
            var steves = collection.Where(x => x.Name == "TestSave");
            Assert.Equal(count, steves.Count());
        }

        [Fact]
        public async Task TestSaveAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var count = 0;
            await foreach (var person in collection.Where(x => x.Name == "Chris"))
            {
                count++;
                person.Name = "TestSaveAsync";
                person.Mother = new Person { Name = "Monica" };
                person.IsEngineer = true;
            }
            await collection.SaveAsync();
            var augustines = collection.Where(x => x.Name == "TestSaveAsync");
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
                person.Name = "TestSaveAsyncSecondEnumeration";
            }

            await collection.SaveAsync();
            var augustines = collection.Where(x => x.Name == "TestSaveAsyncSecondEnumeration");
            Assert.Equal(count, augustines.Count());
        }

        [Fact]
        public async Task TestSaveHashAsync()
        {
            var collection = new RedisCollection<HashPerson>(_connection, 10000);
            var count = 0;
            await foreach (var person in collection)
            {
                count++;
                person.Name = "TestSaveHashAsync";
                person.Mother = new HashPerson { Name = "Diane" };
            }
            await collection.SaveAsync();
            var steves = collection.Where(x => x.Name == "TestSaveHashAsync");
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
            catch (Exception)
            {
                throw;
            }
        }

        [Fact]
        public void TestSaveArrayHash()
        {
            var maryNicknames = new List<string> { "Mary", "Mae", "Mimi", "Mitzi" };
            var maria = new HashPerson { Name = "Maria", NickNames = maryNicknames };
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
            var testP = new Person { Name = "Steve", Home = new GeoLoc(1.0, 1.0) };
            var id = _connection.Set(testP);
            var reconstituded = _connection.Get<Person>(id);
            Assert.Equal("Steve", reconstituded.Name);
            Assert.Equal(new GeoLoc(1.0, 1.0), reconstituded.Home);
        }

        [Fact]
        public void TestNestedObjectQuery()
        {
            var testP = new Person { Name = "Steve", Home = new GeoLoc(1.0, 1.0), Address = new Address { City = "Newark" } };
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            Assert.True(collection.Where(x => x.Name == "Steve" && x.Address.City == "Newark").FirstOrDefault() != default);
        }

        [Fact]
        public void TestNestedObjectQuery2Levels()
        {
            var testP = new Person { Name = "Steve", Home = new GeoLoc(1.0, 1.0), Address = new Address { ForwardingAddress = new Address { City = "Newark" } } };
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            Assert.True(collection.Where(x => x.Name == "Steve" && x.Address.ForwardingAddress.City == "Newark").FirstOrDefault() != default);
        }

        [Fact]
        public void TestArrayQuery()
        {
            var testP = new Person { Name = "Stephen", Home = new GeoLoc(1.0, 1.0), Address = new Address { ForwardingAddress = new Address { City = "Newark" } }, NickNames = new[] { "Steve" } };
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            var steve = collection.FirstOrDefault(x => x.NickNames.Contains("Steve"));
            Assert.Equal(id.Split(':')[1], steve.Id);
        }

        [Fact]
        public void TestArrayQuerySpecialChars()
        {
            var testP = new Person { Name = "Stephen", Home = new GeoLoc(1.0, 1.0), Address = new Address { ForwardingAddress = new Address { City = "Newark" } }, NickNames = new[] { "Steve@redis.com" } };
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            var steve = collection.FirstOrDefault(x => x.NickNames.Contains("Steve@redis.com"));
            Assert.Equal(id.Split(':')[1], steve.Id);
        }

        [Fact]
        public void TestListQuery()
        {
            var testP = new Person { Name = "Stephen", Home = new GeoLoc(1.0, 1.0), Address = new Address { ForwardingAddress = new Address { City = "Newark" } }, NickNamesList = new List<string> { "Stevie" } };
            var id = _connection.Set(testP);
            var collection = new RedisCollection<Person>(_connection);
            var steve = collection.FirstOrDefault(x => x.NickNamesList.Contains("Stevie"));
            Assert.Equal(id.Split(':')[1], steve.Id);
        }

        [Fact]
        public void TestCountWithEmptyCollection()
        {
            var collection = new RedisCollection<ClassForEmptyRedisCollection>(_connection);
            var count = collection.Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void TestUpdate()
        {
            var collection = new RedisCollection<Person>(_connection);
            var testP = new Person { Name = "Steve", Age = 32 };
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
        public void TestUpdateNullCollection()
        {
            var nickNames = new List<string>() { "Bond", "James", "Steve" };
            var collection = new RedisCollection<Person>(_connection);
            var testP = new Person { Name = "Steve", Age = 32 };
            var key = collection.Insert(testP);
            var queriedP = collection.FindById(key);

            queriedP.NickNamesList = nickNames;
            collection.Update(queriedP);

            var secondQueriedP = collection.FindById(key);

            Assert.NotNull(secondQueriedP);
            Assert.Equal(secondQueriedP.NickNamesList, nickNames);
        }

        [Fact]
        public async Task TestUpdateAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var testP = new Person { Name = "Steve", Age = 32 };
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
            var testP = new Person { Name = "Steve", Age = 32 };
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
            var testP = new HashPerson { Name = "Steve", Age = 32 };
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
            var collection = new RedisCollection<Person>(_connection, 10000);
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
            var collection = new RedisCollection<Person>(_connection, 10000);
            Assert.NotNull(await collection.FirstOrDefaultAsync());
        }

        [Fact]
        public async Task TestAnyAsync()
        {
            var collection = new RedisCollection<Person>(_connection, 10000);
            Assert.True(await collection.AnyAsync());
        }

        [Fact]
        public async Task TestSingleAsync()
        {
            var person = new Person { Name = "foo" };
            var collection = new RedisCollection<Person>(_connection, 10000);
            await collection.InsertAsync(person);
            var id = person.Id;
            var res = await collection.SingleAsync(x => x.Id == id);
            Assert.Equal("foo", res.Name);
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
            var person = new Person { Name = "Bob" };
            var collection = new RedisCollection<Person>(_connection);
            await collection.InsertAsync(person);
            var alsoBob = await collection.FindByIdAsync(person.Id);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob", person.Name);
        }

        [Fact]
        public void FindById()
        {
            var person = new Person { Name = "Bob" };
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(person);
            var alsoBob = collection.FindById(person.Id);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob", person.Name);
        }

        [Fact]
        public void FindByIdSavedToStateManager()
        {
            var expectedName = "Bob";
            var person = new Person { Name = "Bob" };
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(person);
            var alsoBob = collection.FindById(person.Id);
            Assert.NotNull(alsoBob);
            Assert.Equal(expectedName, person.Name);
            var dataStateManager = collection.StateManager.Data.FirstOrDefault();
            var snapshotStateManager = collection.StateManager.Snapshot.FirstOrDefault();
            var personInDataStateManager = (Person)dataStateManager.Value;
            var personInSnapshotStateManager = (JObject)snapshotStateManager.Value;
            Assert.Equal(expectedName, personInDataStateManager.Name);
            Assert.Equal(expectedName, personInSnapshotStateManager.Value<string>("Name"));
        }

        [Fact]
        public void FindByKey()
        {
            var person = new Person { Name = "Bob" };
            var collection = new RedisCollection<Person>(_connection);
            var key = collection.Insert(person);
            var alsoBob = collection.FindById(key);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob", person.Name);
        }

        [Fact]
        public async Task FindByKeyAsync()
        {
            var person = new Person { Name = "Bob" };
            var collection = new RedisCollection<Person>(_connection);
            var key = await collection.InsertAsync(person);
            var alsoBob = await collection.FindByIdAsync(key);
            Assert.NotNull(alsoBob);
            Assert.Equal("Bob", person.Name);
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
            var obj = new ObjectWithStringLikeValueTypes() { AnEnum = AnEnum.two, AnEnumAsInt = AnEnum.three };
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
            var obj = new ObjectWithStringLikeValueTypesHash() { AnEnum = AnEnum.two };
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
                Addresses = new[] { new Address { City = "Newark", State = "New Jersey" } },
                AddressList = new List<Address> { new() { City = "Satellite Beach", State = "Florida" } }
            };

            await _connection.SetAsync(obj);

            var collection = new RedisCollection<ObjectWithEmbeddedArrayOfObjects>(_connection);

            var results = await collection.Where(x => x.Addresses.Any(x => x.City == "Newark")).ToListAsync();
            Assert.NotEmpty(results);
            results = await collection.Where(x => x.AddressList.Any(x => x.City == "Satellite Beach")).ToListAsync();
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task TestQueryWithNoStopwords()
        {
            var obj = new ObjectWithZeroStopwords()
            {
                Name = "to be or not to be that is the question"
            };

            await _connection.SetAsync(obj);

            var collection = new RedisCollection<ObjectWithZeroStopwords>(_connection);
            var result = await collection.FirstOrDefaultAsync(x => x.Name == "to be or not to be that is the question");

            Assert.NotNull(result);
        }

        [Fact]
        public async Task FindByIdsAsyncIds()
        {
            var person1 = new Person() { Name = "Alice", Age = 51 };
            var person2 = new Person() { Name = "Bob", Age = 37 };

            var collection = new RedisCollection<Person>(_connection);

            await collection.InsertAsync(person1);
            await collection.InsertAsync(person2);

            var ids = new string[] { person1.Id, person2.Id };

            var people = await collection.FindByIdsAsync(ids);
            Assert.NotNull(people[ids[0]]);
            Assert.Equal(ids[0], people[ids[0]].Id);
            Assert.Equal("Alice", people[ids[0]].Name);
            Assert.Equal(51, people[ids[0]].Age);

            Assert.NotNull(people[ids[1]]);
            Assert.Equal(ids[1], people[ids[1]].Id);
            Assert.Equal("Bob", people[ids[1]].Name);
            Assert.Equal(37, people[ids[1]].Age);
        }

        [Fact]
        public async Task FindByIdsAsyncIdsWithDuplicatedIds()
        {
            var person1 = new Person() { Name = "Alice", Age = 51 };
            var person2 = new Person() { Name = "Bob", Age = 37 };

            var collection = new RedisCollection<Person>(_connection);

            await collection.InsertAsync(person1);
            await collection.InsertAsync(person2);

            var ids = new string[] { person1.Id, person2.Id, person1.Id, person2.Id };

            var people = await collection.FindByIdsAsync(ids);
            Assert.NotNull(people[ids[0]]);
            Assert.Equal(ids[0], people[ids[0]].Id);
            Assert.Equal("Alice", people[ids[0]].Name);
            Assert.Equal(51, people[ids[0]].Age);

            Assert.NotNull(people[ids[1]]);
            Assert.Equal(ids[1], people[ids[1]].Id);
            Assert.Equal("Bob", people[ids[1]].Name);
            Assert.Equal(37, people[ids[1]].Age);
        }

        [Fact]
        public async Task FindByIdsAsyncKeys()
        {
            var person1 = new Person() { Name = "Alice", Age = 51 };
            var person2 = new Person() { Name = "Bob", Age = 37 };

            var collection = new RedisCollection<Person>(_connection);

            var key1 = await collection.InsertAsync(person1);
            var key2 = await collection.InsertAsync(person2);

            var keys = new string[] { key1, key2 };

            var people = await collection.FindByIdsAsync(keys);
            Assert.NotNull(people[keys[0]]);
            Assert.Equal(keys[0].Split(':').Last(), people[keys[0]]!.Id);
            Assert.Equal("Alice", people[keys[0]].Name);
            Assert.Equal(51, people[keys[0]].Age);

            Assert.NotNull(people[keys[1]]);
            Assert.Equal(keys[1].Split(':').Last(), people[keys[1]]!.Id);
            Assert.Equal("Bob", people[keys[1]].Name);
            Assert.Equal(37, people[keys[1]].Age);
        }

        [Fact]
        public async Task TestSaveAfterAsyncMethods()
        {
            var collection = new RedisCollection<Person>(_connection);
            var people = collection.Take(50).ToArray();
            var i = 0;
            //byId
            collection = new RedisCollection<Person>(_connection);
            var person = people[i];
            var byId = await collection.FindByIdAsync(person.Id);
            byId!.Name = "Changed";
            collection.Save();
            byId = await collection.FindByIdAsync(person.Id);
            Assert.Equal("Changed", byId.Name);

            //first
            i++;
            collection = new RedisCollection<Person>(_connection);
            person = people[i];
            byId = await collection.FirstAsync(x => x.Id == person.Id);
            byId!.Name = "Changed";
            collection.Save();
            byId = await collection.FindByIdAsync(person.Id);
            Assert.Equal("Changed", byId.Name);

            //firstOrDefault
            i++;
            collection = new RedisCollection<Person>(_connection);
            person = people[i];
            byId = await collection.FirstOrDefaultAsync(x => x.Id == person.Id);
            byId!.Name = "Changed";
            collection.Save();
            byId = await collection.FindByIdAsync(person.Id);
            Assert.Equal("Changed", byId.Name);

            //Single
            i++;
            collection = new RedisCollection<Person>(_connection);
            person = people[i];
            byId = await collection.SingleAsync(x => x.Id == person.Id);
            byId!.Name = "Changed";
            collection.Save();
            byId = await collection.FindByIdAsync(person.Id);
            Assert.Equal("Changed", byId.Name);

            //SingleOrDefault
            i++;
            collection = new RedisCollection<Person>(_connection);
            person = people[i];
            byId = await collection.SingleOrDefaultAsync(x => x.Id == person.Id);
            byId!.Name = "Changed";
            collection.Save();
            byId = await collection.FindByIdAsync(person.Id);
            Assert.Equal("Changed", byId.Name);

            //byIds
            i++;
            collection = new RedisCollection<Person>(_connection);
            var thePeople = people.Skip(i).Take(5);
            var ids = thePeople.Select(x => x.Id);
            var byIds = (await collection.FindByIdsAsync(ids)).Values;
            foreach (var p in byIds)
            {
                if (p == null)
                    continue;
                p.Name = "Changed";
            }

            collection.Save();

            byIds = (await collection.FindByIdsAsync(ids)).Values;
            foreach (var p in byIds)
            {
                Assert.Equal("Changed", p.Name);
            }
        }

        [Fact]
        public async Task TestMultipleContains()
        {
            var collection = new RedisCollection<Person>(_connection);
            var person1 = new Person() { Name = "Alice", Age = 51, NickNames = new[] { "Ally", "Alie", "Al" } };
            var person2 = new Person() { Name = "Robert", Age = 37, NickNames = new[] { "Bobby", "Rob", "Bob" } };

            await collection.InsertAsync(person1);
            await collection.InsertAsync(person2);

            var people = await collection.Where(x => x.NickNames.Contains("Bob") || x.NickNames.Contains("Alie")).ToListAsync();

            Assert.Contains(people, x => x.Id == person1.Id);
            Assert.Contains(people, x => x.Id == person2.Id);
        }

        [Fact]
        public async Task TestMultipleContainsGuid()
        {
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var objectList = Enumerable.Range(1, 10).Select(x => new ObjectWithStringLikeValueTypes() { Guid = Guid.NewGuid() }).ToList();
            foreach (var item in objectList)
            {
                await collection.InsertAsync(item);
            }

            var ids = objectList.Select(x => x.Guid);
            var objects = await collection.Where(x => ids.Contains(x.Guid)).ToListAsync();

            Assert.Equal(ids, objects.Select(x => x.Guid));
        }

        [Fact]
        public async Task TestShouldFailForSave()
        {
            var expectedText = "The RedisCollection has been instructed to not maintain the state of records enumerated by " +
                               "Redis making the attempt to Save Invalid. Please initialize the RedisCollection with saveState " +
                               "set to true to Save documents in the RedisCollection";
            var collection = new RedisCollection<Person>(_connection, false, 100);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(collection.SaveAsync().AsTask);
            Assert.Equal(expectedText, ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => collection.Save());
            Assert.Equal(expectedText, ex.Message);
        }

        [Fact]
        public async Task TestStatelessCollection()
        {
            var collection = new RedisCollection<Person>(_connection, false, 10000);
            var res = await collection.ToListAsync();
            Assert.True(res.Count >= 1);
            Assert.Equal(0, collection.StateManager.Data.Count);
            Assert.Equal(0, collection.StateManager.Snapshot.Count);
        }

        [Fact]
        public async Task TestFlagEnumQuery()
        {
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection, false, 10000);
            var obj = new ObjectWithStringLikeValueTypes { Flags = EnumFlags.One | EnumFlags.Two };
            await collection.InsertAsync(obj);
            var res = await collection.FirstOrDefaultAsync(x => x.Flags == EnumFlags.One);
            Assert.NotNull(res);
        }

        public void CompareTimestamps(DateTime ts1, DateTime ts2)
        {
            Assert.Equal(ts1.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), ts2.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task TestTimeStampRanges()
        {
            var collection = new RedisCollection<ObjectWithDateTime>(_connection, false, 10000);
            var timestamp = DateTime.Now;
            var greaterThanTimestamp = DateTime.Now.Subtract(TimeSpan.FromHours(1));
            var unixTimestamp = new DateTimeOffset(timestamp).ToUnixTimeMilliseconds();
            var obj = new ObjectWithDateTime { Timestamp = timestamp, NullableTimestamp = timestamp };
            var id = await collection.InsertAsync(obj);
            var first = await collection.FirstOrDefaultAsync(x => x.Timestamp > greaterThanTimestamp);
            Assert.NotNull(first);
            Assert.NotNull(first.NullableTimestamp);
            CompareTimestamps(timestamp, first.Timestamp);
            CompareTimestamps(timestamp, first.NullableTimestamp.Value);
            Assert.Equal(obj.Id, first.Id);
        }

        [Fact]
        public async Task TestTimeStampRangesHash()
        {
            var collection = new RedisCollection<ObjectWithDateTimeHash>(_connection, false, 10000);
            var timestamp = DateTime.Now;
            var greaterThanTimestamp = DateTime.Now.Subtract(TimeSpan.FromHours(1));
            var unixTimestamp = new DateTimeOffset(timestamp).ToUnixTimeMilliseconds();
            var obj = new ObjectWithDateTimeHash { Timestamp = timestamp, NullableTimestamp = timestamp };
            var id = await collection.InsertAsync(obj);
            var first = await collection.FirstOrDefaultAsync(x => x.Timestamp > greaterThanTimestamp);
            Assert.NotNull(first);
            Assert.NotNull(first.NullableTimestamp);
            CompareTimestamps(timestamp, first.Timestamp);
            CompareTimestamps(timestamp, first.NullableTimestamp.Value);
            Assert.Equal(obj.Id, first.Id);
        }

        [Fact]
        public async Task TestListContains()
        {
            var collection = new RedisCollection<Person>(_connection);
            var person1 = new Person() { Name = "Ferb", Age = 14, NickNames = new[] { "Feb", "Fee" } };
            var person2 = new Person() { Name = "Phineas", Age = 14, NickNames = new[] { "Phineas", "Triangle Head", "Phine" } };

            await collection.InsertAsync(person1);
            await collection.InsertAsync(person2);

            var names = new List<string> { "Ferb", "Phineas" };
            var people = await collection.Where(x => names.Contains(x.Name)).ToListAsync();

            Assert.Contains(people, x => x.Id == person1.Id);
            Assert.Contains(people, x => x.Id == person2.Id);
        }

        [Fact]
        public async Task TestListMultipleContains()
        {
            var collection = new RedisCollection<Person>(_connection);
            var person1 = new Person() { Name = "Ferb", Age = 14, NickNames = new[] { "Feb", "Fee" }, TagField = "Ferb" };
            var person2 = new Person() { Name = "Phineas", Age = 14, NickNames = new[] { "Phineas", "Triangle Head", "Phine" }, TagField = "Phineas" };

            await collection.InsertAsync(person1);
            await collection.InsertAsync(person2);

            var names = new List<string> { "Ferb", "Phineas" };
            var ages = new List<int?> { 14, 50, 60 };
            var people = await collection.Where(x => names.Contains(x.Name) && names.Contains(x.TagField) && ages.Contains(x.Age)).ToListAsync();

            Assert.Contains(people, x => x.Id == person1.Id);
            Assert.Contains(people, x => x.Id == person2.Id);
        }

        [Fact]
        public void TestGuidSelects()
        {
            var obj = new ObjectWithStringLikeValueTypes { Guid = Guid.NewGuid() };
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var key = collection.Insert(obj);
            var res = collection.Where(x => x.Guid == obj.Guid).Select(x => x.Guid).ToList();
            Assert.NotEmpty(res);
            _connection.Unlink(key);
        }

        [Fact]
        public void TestUlidSelects()
        {
            var obj = new ObjectWithStringLikeValueTypes { Ulid = Ulid.NewUlid() };
            var collection = new RedisCollection<ObjectWithStringLikeValueTypes>(_connection);
            var key = collection.Insert(obj);
            var res = collection.Where(x => x.Ulid == obj.Ulid).Select(x => x.Ulid).ToList();
            Assert.NotEmpty(res);
            _connection.Unlink(key);
        }

        [Fact]
        public void TestIntSelects()
        {
            var obj = new Person { Name = "steve", Age = 33};
            var collection = new RedisCollection<Person>(_connection);
            collection.Insert(obj);
            var res = collection.Where(x => x.Age == obj.Age).Select(x => x.Age).ToList();
            Assert.NotEmpty(res);
            collection.Delete(obj);
        }

        [Fact]
        public void TestQueryWithForwardSlashes()
        {
            var collection = new RedisCollection<ObjectWithZeroStopwords>(_connection);
            collection.Insert(new ObjectWithZeroStopwords() { Name = "a/test/string" });

            Assert.NotNull(collection.FirstOrDefault(x=>x.Name == "a/test/string"));
        }
        
        [Fact]
        public void TestComplexObjectsWithMixedNesting()
        {
            var obj = new ComplexObjectWithCascadeAndJsonPath
            {
                InnerCascade = new InnerObject()
                {
                    InnerInnerCascade = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 },
                    InnerInnerCollection = new[] { new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 } },
                    InnerInnerJson = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 }
                },
                InnerJson = new InnerObject()
                {
                    InnerInnerCascade = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 },
                    InnerInnerCollection = new[] { new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 } },
                    InnerInnerJson = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 }
                }
            };

            var collection = new RedisCollection<ComplexObjectWithCascadeAndJsonPath>(_connection);

            collection.Insert(obj);

            Assert.NotNull(collection.FirstOrDefault(x => x.InnerCascade.InnerInnerCascade.Tag == "World"));
            Assert.NotNull(collection.FirstOrDefault(x=> x.InnerCascade.InnerInnerCascade.Num == 42));
            Assert.NotNull(collection.FirstOrDefault(x=> x.InnerCascade.InnerInnerCollection.Any(x=>x.Tag == "World")));
            Assert.NotNull(collection.FirstOrDefault(x=>x.InnerJson.InnerInnerCascade.Tag == "World"));
            Assert.NotNull(collection.FirstOrDefault(x=>x.InnerJson.InnerInnerCascade.Arr.Contains("hello")));
        }
        
        [Fact]
        public void TestUpdateWithQuotes()
        {
            var obj = new BasicJsonObject() { Name = "Bob" };
            var collection = new RedisCollection<BasicJsonObject>(_connection);
            collection.Insert(obj);
            var reconstituted = collection.FindById(obj.Id);
            reconstituted.Name = "\"Bob";
            collection.Update(reconstituted);
            collection.Delete(obj);
        }

        [Fact]
        public void TestSelectOnEmbeddedDocuments()
        {
            var inner1 = new InnerObject()
            {
                InnerInnerCascade = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 },
                InnerInnerCollection = new[]
                    { new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 } },
                InnerInnerJson = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 }
            };

            var inner2 = new InnerObject()
            {
                InnerInnerCascade = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 },
                InnerInnerCollection = new[]
                    { new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 } },
                InnerInnerJson = new InnerInnerObject() { Arr = new[] { "hello" }, Tag = "World", Num = 42 }
            };

            var collection = new RedisCollection<SelectTestObject>(_connection);
            collection.Insert(new SelectTestObject
            {
                Field1 = inner1,
                Field2 = inner2
            });
            
            var resSimpleAnonNew = collection.Select(x => new {x.Field1, x.Field2}).ToList().First();
            Assert.Equal("World",resSimpleAnonNew.Field1.InnerInnerCascade.Tag);
            Assert.Equal(42,resSimpleAnonNew.Field1.InnerInnerCascade.Num);
            Assert.Equal("World",resSimpleAnonNew.Field2.InnerInnerCascade.Tag);
            Assert.Equal(42,resSimpleAnonNew.Field2.InnerInnerCascade.Num);
            
            var resAnonWithAssignments = collection.Select(x => new {Field3 = x.Field1, Field4 = x.Field2}).ToList().First();
            Assert.Equal("World",resAnonWithAssignments.Field3.InnerInnerCascade.Tag);
            Assert.Equal(42,resAnonWithAssignments.Field3.InnerInnerCascade.Num);
            Assert.Equal("World",resAnonWithAssignments.Field4.InnerInnerCascade.Tag);
            Assert.Equal(42,resAnonWithAssignments.Field4.InnerInnerCascade.Num);
            
            var resWithOtherObject = collection.Select(x => new CongruentObject{Field3 = x.Field1, Field4 = x.Field2}).ToList().First();
            Assert.Equal("World",resWithOtherObject.Field3.InnerInnerCascade.Tag);
            Assert.Equal(42,resWithOtherObject.Field3.InnerInnerCascade.Num);
            Assert.Equal("World",resWithOtherObject.Field4.InnerInnerCascade.Tag);
            Assert.Equal(42,resWithOtherObject.Field4.InnerInnerCascade.Num);
            
            var resWithOtherObjectLikeNames = collection.Select(x => new CongruentObjectWithLikeNames(){Field1 = x.Field1, Field2 = x.Field2}).ToList().First();
            Assert.Equal("World",resWithOtherObjectLikeNames.Field1.InnerInnerCascade.Tag);
            Assert.Equal(42,resWithOtherObjectLikeNames.Field1.InnerInnerCascade.Num);
            Assert.Equal("World",resWithOtherObjectLikeNames.Field2.InnerInnerCascade.Tag);
            Assert.Equal(42,resWithOtherObjectLikeNames.Field2.InnerInnerCascade.Num);

            var resNoNew = collection.Select(x => x.Field1).ToList().First();
            Assert.Equal("World",resNoNew.InnerInnerCascade.Tag);
            Assert.Equal(42,resNoNew.InnerInnerCascade.Num);
        }
    }
}