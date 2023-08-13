using System;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using System.Threading.Tasks;
using System.Linq;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Collection("Redis")]
    public class AggregationFunctionalTests
    {
        private static readonly object connectionLock = new();

        /// <summary>
        /// Init the database taking care to do it only once so that the test processes performed in parallel can count on an immutable database
        /// </summary>
        /// <param name="setup">RedisSetup object instance</param>
        public AggregationFunctionalTests(RedisSetup setup)
        {
            _connection = setup.Connection;
        }
        private IRedisConnection _connection;

        private void Setup()
        {
            var beaker = new Person
            {
                Name = "Beaker",
                Age = 23,
                Sales = 500000,
                SalesAdjustment = .6,
                Height = 15,
                DepartmentNumber = 3                
            };

            var bunsen = new Person
            {
                Name = "Dr Bunsen Honeydew",
                Age = 63,
                Sales = 500000,
                SalesAdjustment = .6,
                Height = 15,
                DepartmentNumber = 3
            };

            var fozzie = new Person
            {
                Name = "Fozzie Bear",
                Age = 45,
                Sales = 350000,
                SalesAdjustment = .7,
                Height = 14,
                DepartmentNumber = 2
            };

            var startler = new Person
            {
                Name = "Statler",
                Age = 75,
                Sales = 650000,
                SalesAdjustment = .8,
                Height = 13,
                DepartmentNumber = 4
            };

            var waldorf = new Person
            {
                Name = "Waldorf",
                Age = 78,
                Sales = 750000,
                SalesAdjustment = .8,
                Height = 13,
                DepartmentNumber = 4
            };

            var kermit = new Person
            {
                Name = "Kermit the Frog",
                Age = 52,
                Sales = 1500000,
                SalesAdjustment = .8,
                Height = 13,
                DepartmentNumber = 1
            };
            
            var hashWaldorf = new HashPerson
            {
                Name = "Waldorf",
                Email = "Waldorf@muppets.com",
                Age = 78,
                Sales = 750000,
                SalesAdjustment = .8,
                Height = 13,
                DepartmentNumber = 4
            };

            var hashKermit = new HashPerson
            {
                Name = "Kermit the Frog",
                Email = "kermit@muppets.com",
                Age = 52,
                Sales = 1500000,
                SalesAdjustment = .8,
                Height = 13,
                DepartmentNumber = 1
            };

            _connection.Set(kermit);
            _connection.Set(waldorf);
            _connection.Set(startler);
            _connection.Set(fozzie);
            _connection.Set(beaker);
            _connection.Set(bunsen);
            _connection.Set(hashKermit);
            _connection.Set(hashWaldorf);
        }


        [Fact]
        public void GetDepartmentBySales()
        {
            var collection = new RedisAggregationSet<Person>(_connection);
            var departments = collection
                .Apply(x => x.RecordShell.Sales * x.RecordShell.SalesAdjustment, "AdjustedSales")
                .GroupBy(x => x.RecordShell.DepartmentNumber)
                .Sum(x => x["AdjustedSales"])
                .OrderByDescending(x => x["AdjustedSales_SUM"])
                .ToArray();
            Assert.Equal(1, (int)departments[0]["DepartmentNumber"]);
            Assert.Equal(4, (int)departments[1]["DepartmentNumber"]);
            Assert.Equal(3, (int)departments[2]["DepartmentNumber"]);
            Assert.Equal(2, (int)departments[3]["DepartmentNumber"]);
        }

        [Fact]
        public void GetHandicappedSales()
        {
            var collection = new RedisAggregationSet<Person>(_connection);
            var employees = collection.Apply(x => x.RecordShell.Sales 
                * x.RecordShell.SalesAdjustment, "AdjustedSales")
                .OrderByDescending(x=>x["AdjustedSales"]);
            var previousEmployee = employees.First();
            foreach(var employee in employees)
            {
                Assert.True(employee["AdjustedSales"] <= previousEmployee["AdjustedSales"]);
                previousEmployee = employee;
            }
        }

        [Fact]
        public async Task GetAdjustedSalesStandardDeviation()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            var stddev = await collection.Apply(x => x.RecordShell.Sales
                * x.RecordShell.SalesAdjustment, "AdjustedSales")
                .StandardDeviationAsync(x => x["AdjustedSales"]);

            Assert.Equal(358018.854252, stddev);
        }

        [Fact]

        public void GetAdjustedSalesStandardDeviationTestingInvariantCultureCompliance()
        {
            Helper.RunTestUnderDifferentCulture("it-IT", x => GetAdjustedSalesStandardDeviation());
        }

        [Fact]
        public async Task GetAverageAdjustedSales()
        {
            var collection = new RedisAggregationSet<Person>(_connection);
            var average = await collection.Apply(x => x.RecordShell.Sales
                                                      * x.RecordShell.SalesAdjustment, "AdjustedSales")
                .AverageAsync(x => x["AdjustedSales"]);
            Assert.Equal(527500, average);
        }

        [Fact]
        public async Task TestLoad()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            await foreach (var result in collection.Load(x => x.RecordShell.Name))
            {
                Assert.False(string.IsNullOrEmpty(result["Name"]));
            }
        }
        
        [Fact]
        public async Task TestLoadAll()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            await foreach (var result in collection.LoadAll())
            {
                Assert.NotNull(result.Hydrate().Name);
            }
        }

        [Fact]
        public async Task TestPartialHydration()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            await foreach (var result in collection.Apply(x => x.RecordShell.Sales * x.RecordShell.SalesAdjustment, "AdjustedSales"))
            {
                var partialHydration = result.Hydrate();
                Assert.True(partialHydration.Sales > 0);
                Assert.True(partialHydration.SalesAdjustment > 0);
            }
        }

        [Fact]
        public async Task TestHydrateHash()
        {
            Setup();
            var collection = new RedisAggregationSet<HashPerson>(_connection);
            await foreach (var result in collection.LoadAll())
            {
                Assert.NotNull(result.Hydrate().Name);
            }
        }

        [Fact]
        public async Task TestPartialHydrationHash()
        {
            Setup();
            var collection = new RedisAggregationSet<HashPerson>(_connection);
            await foreach (var result in collection.Load(x=>x.RecordShell.Email))
            {
                Assert.NotNull(result.Hydrate().Email);
            }
        }

        [Fact]
        public async Task GetGroupCount()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            var results = await collection.GroupBy(x => x.RecordShell.Age).CountGroupMembers().ToListAsync();
            foreach (var result in results)
            {
                Assert.True(1<=result["COUNT"]);
            }
        }
        
        [Fact]
        public async Task GetGroupCountWithNegationQuery()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            var results = await collection
                .Where(x => x.RecordShell.Age != 0)
                .GroupBy(x => x.RecordShell.Age).CountGroupMembers().ToListAsync();
            foreach (var result in results)
            {
                Assert.True(1 <= result["COUNT"]);
            }
        }

        [Fact]
        public void TestUnsortedFields()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);

            Assert.Throws<NotSupportedException>(() =>
                collection.Apply(x => $"{x.RecordShell.Email}", "TheEmailThatNeverWas").ToList());

        }
    }
}
