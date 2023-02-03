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
        public async Task Test_Bulk_InsertAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var persons = new List<Person>() {
                new Person() { Name = "Alice", Age = 51, NickNames = new[] { "Ally", "Alie", "Al" } },
                new Person() { Name = "Robert", Age = 37, NickNames = new[] { "Bobby", "Rob", "Bob" } },
                new Person() { Name = "Jeeva", Age = 22, NickNames = new[] { "Jee", "Jeev", "J" } },
                new Person() { Name = "Martin", Age = 60, NickNames = new[] { "Mart", "Mat", "tin" } }
                };
            var keys = await collection.Insert(persons);

            var people = collection.Where(x => x.NickNames.Contains("Bob") || x.NickNames.Contains("Alie")).ToList();
            Assert.Contains(people, x => x.Name == persons.First().Name);
        }

        [Fact]
        public async Task Test_Inserts_TwiceWith_SaveDataWith_ExactFields()
        {
            var collection = new RedisCollection<Person>(_connection);
            var persons = new List<Person>() {
                new Person() { Name = "Alice", Age = 14, NickNames = new[] { "Ally", "Alie", "Al" } },
                new Person() { Name = "Robert", Age = 30, NickNames = new[] { "Bobby", "Rob", "Bob" } },
                new Person() { Name = "Jeeva", Age = 22, NickNames = new[] { "Jee", "Jeev", "J" } },
                new Person() { Name = "Martin", Age = 61, NickNames = new[] { "Mart", "Mat", "tin" } }
                };
            var keys = await collection.Insert(persons); //performs JSON.SET create keys and emit the list of keys.

            var persons2 = new List<Person>() {
                new Person() { Name = "Alice", Age = 14, NickNames = new[] { "Ally", "Alie", "Al" }, IsEngineer = true },
                new Person() { Name = "Robert", Age = 30, NickNames = new[] { "Bobby", "Rob", "Bob" }, IsEngineer = false },
                new Person() { Name = "Jeeva", Age = 22, NickNames = new[] { "Jee", "Jeev", "J" }, DepartmentNumber = 201 },
                new Person() { Name = "Martin", Age = 61, NickNames = new[] { "Mart", "Mat", "tin" }, TagField = "Martin" }
                };

            var keys2 = await collection.Insert(persons2); //create keys and emit the list of keys.

            var people = collection.Where(x => x.Age >= 20 && x.Age <=30).ToList();
            Assert.NotEqual(keys, keys2); //not performs any re-indexing because keys are not same.
        }

        [Fact]
        public async Task Test_BulkInsert_WithSameIds()
        {
            var collection = new RedisCollection<Person>(_connection);
            var persons = new List<Person>() {
                new Person() {Id="01GFZ9Y6CTEDHHXKT055N1YP3A" , Name = "Alice", Age = 51, NickNames = new[] { "Ally", "Alie", "Al" } },
                new Person() {Id="01GFZ9Y6CTEDHHXKT055N1YP3A" , Name = "Jeevananthan", Age = 37, NickNames = new[] { "Jeeva", "Jee"} },
                new Person() { Name = "Jeeva", Age = 22, NickNames = new[] { "Jee", "Jeev", "J" }, },
                new Person() { Name = "Martin", Age = 60, NickNames = new[] { "Mart", "Mat", "tin" }, }
                };
             await collection.Insert(persons);
            var people = collection.Where(x => x.NickNames.Contains("Jeeva") || x.NickNames.Contains("Alie")).ToList();
            Assert.False(people.First().Name == persons.First().Name); // this fails because the Name field of people doesn't contains the Name value Alice
        }

        [Fact]
        public async Task Test_BulkInsert_HashesWith_Expiration()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var PhineasFerb = new List<HashPerson>() {
                new HashPerson() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" },
                new HashPerson() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation" }
            };

             await collection.Insert(PhineasFerb, TimeSpan.FromMilliseconds(8000));
            var ttl = (long)_connection.Execute("PTTL", PhineasFerb[0].GetKey());
            Assert.True(ttl <= 8000);
            Assert.True(ttl >= 1000);
        }

        [Fact]
        public async Task Test_BulkInsert_WithExpiration()
        {
            var collection = new RedisCollection<Person>(_connection);
            var PhineasFerb = new List<Person>() {
                new Person() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" ,  NickNames = new[] { "Feb", "Fee" } },
                new Person() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation",  NickNames = new[] { "Phineas", "Triangle Head", "Phine" } }
            };

            await collection.Insert(PhineasFerb, TimeSpan.FromSeconds(8));
            var ttl = (long)_connection.Execute("PTTL", PhineasFerb[0].GetKey());
            Assert.True(ttl <= 8000);
            Assert.True(ttl >= 1000);
        }

        [Fact]
        public async Task Test_Bulk_Insert_Del()
        {
            var collection = new RedisCollection<Person>(_connection);
            var PhineasFerbShow = new List<Person>() {
                new Person() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" , Address = new Address { State = "Tri-State Area"} },
                new Person() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation", Address = new Address { State = "Tri-State Area"} },
                new Person() { Name = "Dr.Doofenshmirtz", Age = 38, IsEngineer = true, TagField = "Villain", Address = new Address { State = "Tri-State Area"} },
                new Person() { Name = "Perry", Age = 5, IsEngineer = false, TagField = "Agent", Address = new Address { State = "Tri-State Area "} }
            };

            await collection.Insert(PhineasFerbShow);
            var searchByState = collection.Where(x => x.Address.State == "Tri-State Area").ToList();
            await collection.DeleteAsync(searchByState);
            var searchByTag = collection.FindById(searchByState[0].GetKey());
            Assert.Null(searchByTag);
        }

        [Fact]
        public async Task Test_Bulk_InsertAsync_DelAsync_ForHashes()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var PhineasFerbShow = new List<HashPerson>() {
                new HashPerson() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" , Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation", Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Dr.Doofenshmirtz", Age = 38, IsEngineer = true, TagField = "Villain", Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Perry", Age = 5, IsEngineer = false, TagField = "Agent", Address = new Address { State = "Tri-State Area "} }
            };

            await collection.Insert(PhineasFerbShow);
            var searchByName = await collection.Where(x => x.Name == "Dr.Doofenshmirtz" || x.Name == "Perry").ToListAsync();
            await collection.DeleteAsync(searchByName);
            var searchByTag = await collection.FindByIdAsync(searchByName[0].GetKey());
            Assert.Null(searchByTag);
        }

        [Fact]
        public async Task Test_Bulk_UpdateAsync()
        {
            var collection = new RedisCollection<Person>(_connection);
            var onepiece = new List<Person>() {
                new Person() { Name = "Monkey D.Luffy", Age = 22, NickNames = new[] { "Luffy", "Straw Hat", "GumGum" }, TagField = "The Straw Hat Pirates" },
                new Person() { Name = "Roronano Zoro", Age = 26, NickNames = new[] { "Zoro", "Roronano", "Pirate Hunter" } , TagField = "The Straw Hat Pirates" },
                new Person() { Name = "Monkey D. Garp", Age = 70, NickNames = new[] { "Garp", "Garps", "Hero of the Navy" }, TagField = "Navy" },
                new Person() { Name = "Shanks", Age = 50, NickNames = new[] { "Shanks", "Red-Hair" }, TagField = "Red-Haired Pirates" }
                };
            var keys = await collection.Insert(onepiece);
            var people = collection.Where(x => x.NickNames.Contains("Luffy") || x.NickNames.Contains("Shanks")).ToList();
            Assert.Equal(onepiece[0].Age, people[0].Age);
            people[0].Age = 25;
            people[1].Age = 52;
            await collection.UpdateAsync(people);
            Assert.NotEqual(onepiece[0].Age, people[0].Age);
        }

        [Fact]
        public async Task Test_Bulk_UpdateSync_WithHashesNumeric()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var onepiece = new List<HashPerson>() {
                new HashPerson() { Name = "Monkey D.Luffy", Age = 22, NickNames = new List<string> { "Luffy", "Straw Hat", "GumGum" }, TagField = "The Straw Hat Pirates" },
                new HashPerson() { Name = "Roronano Zoro", Age = 26, NickNames = new List<string> { "Zoro", "Roronano", "Pirate Hunter" } , TagField = "The Straw Hat Pirates" },
                new HashPerson() { Name = "Monkey D. Garp", Age = 70, NickNames = new List<string> { "Garp", "Garps", "Hero of the Navy" }, TagField = "Navy" },
                new HashPerson() { Name = "Shanks", Age = 50, NickNames = new List<string> { "Shanks", "Red-Hair" }, TagField = "Red-Haired Pirates" }
                };
            var keys = collection.Insert(onepiece);
            var people = collection.Where(x => x.Name.Contains("Luffy") || x.Name.Contains("Shanks")).ToList();
            Assert.Equal(onepiece[0].Age, people[0].Age);
            people[0].Height = 20.2;
            people[0].Age = 25;
            people[1].Age = 52;
            await collection.UpdateAsync(people);
            Assert.NotEqual(onepiece[0].Age, people[0].Age);
        }


        [Fact]
        public async Task Test_BulkUpdate_WithEmbbedObject()
        {
            var collection = new RedisCollection<Person>(_connection);
            var onepiece = new List<Person>() {
                new Person() { Name = "Monkey D.Luffy", Age = 22, NickNames = new[] { "Luffy", "Straw Hat", "GumGum" }, TagField = "The Straw Hat Pirates" },
                new Person() { Name = "Roronano Zoro", Age = 26, NickNames = new[] { "Zoro", "Roronano", "Pirate Hunter" } , TagField = "The Straw Hat Pirates" },
                new Person() { Name = "Monkey D. Garp", Age = 70, NickNames = new[] { "Garp", "Garps", "Hero of the Navy" }, TagField = "Navy" },
                new Person() { Name = "Shanks", Age = 50, NickNames = new[] { "Shanks", "Red-Hair" }, TagField = "Red-Haired Pirates" }
                };
            var keys =  collection.Insert(onepiece);
            var people = collection.Where(x => x.NickNames.Contains("Luffy") || x.NickNames.Contains("Shanks")).ToList();
            people[0].Address = new Address { City = "Goa Kingdom" };
            people[1].Address = new Address { City = "Goa Kingdom" };
             await collection.UpdateAsync(people);
            Assert.Contains(people, x => x.Name == onepiece.First().Name);
        }

        [Fact]
        public async Task Test_Bulk50_Records_Insert_Update_Del_Async()
        {
            var collection = new RedisCollection<Person>(_connection, false, 100); // consider using SaveState = false to avoid Concurrent issue

            var names = new[] { "Hassein", "Zoro", "Aegorn", "Jeeva", "Ajith", "Joe", "Mark", "Otto" };
            var rand = new Random();
            var people = new List<Person>();
            for (var i = 0; i < 50; i++)
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
            var keys = await collection.Insert(people); // 1000 records in an avg of 200ms.
            var listofPeople = (await collection.FindByIdsAsync(keys)).Values.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                listofPeople[i].Name = names[rand.Next(0, names.Length)];
                listofPeople[i].DepartmentNumber = rand.Next(5, 9);
                listofPeople[i].Sales = rand.Next(10000, 20000);
                listofPeople[i].Age = rand.Next(30, 50);
                listofPeople[i].Height = 50.0 + rand.NextDouble() * 15;
                listofPeople[i].SalesAdjustment = rand.NextDouble();
            }
            await collection.UpdateAsync(listofPeople); // 1000 records in an avg of 300ms.
            var oldPeople = collection.Where(x => x.Age >= 17 && x.Age <= 21).ToList();
            var newPeople = collection.Where(x => x.Age >= 30 && x.Age <= 50).ToList();
            await collection.DeleteAsync(newPeople); // del
            Assert.Empty(oldPeople);
            Assert.DoesNotContain(people[0], newPeople);
        }

        [Fact]
        public async Task TestBulk_Insert_Update_Del_Async_WithHashes()
        {
            var collection = new RedisCollection<HashPerson>(_connection);
            var PhineasFerbShow = new List<HashPerson>() {
                new HashPerson() { Name = "Ferb", Age = 14, IsEngineer = true, TagField = "SummerVacation" , Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Phineas", Age = 14, IsEngineer = true, TagField = "SummerVacation", Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Dr.Doofenshmirtz", Age = 38, IsEngineer = true, TagField = "Villain", Address = new Address { State = "Tri-State Area"} },
                new HashPerson() { Name = "Perry", Age = 5, IsEngineer = false, TagField = "Agent", Address = new Address { State = "Tri-State Area "} }
            };

            await collection.Insert(PhineasFerbShow);
            var searchByName = await collection.Where(x => x.Name == "Dr.Doofenshmirtz" || x.Name == "Perry").ToListAsync(); 
            searchByName[0].TagField = "Vacation";
            searchByName[1].DepartmentNumber = 2;
            await collection.UpdateAsync(searchByName);
            await collection.DeleteAsync(searchByName);
            var searchByTag = await collection.FindByIdAsync(searchByName[0].GetKey());
            Assert.Null(searchByTag);
        }
    }   
}
