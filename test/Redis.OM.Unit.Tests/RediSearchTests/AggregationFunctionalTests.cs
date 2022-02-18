using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using System.Linq;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Collection("Redis")]
    public class AggregationFunctionalTests
    {
        private static IRedisConnection _connection;
        private static readonly object connectionLock = new();

        /// <summary>
        /// Init the database taking care to do it only once so that the test processes performed in parallel can count on an immutable database
        /// </summary>
        /// <param name="setup">RedisSetup object instance</param>
        public AggregationFunctionalTests(RedisSetup setup)
        {
            if (_connection != null)
                return;

            lock(connectionLock)
            {
                _connection = setup.Connection;

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

                _connection.Set(kermit);
                _connection.Set(waldorf);
                _connection.Set(startler);
                _connection.Set(fozzie);
                _connection.Set(beaker);
                _connection.Set(bunsen);
            }
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
        public async void GetAdjustedSalesStandardDeviation()
        {
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
        public async void GetAverageAdjustedSales()
        {
            var collection = new RedisAggregationSet<Person>(_connection);
            var average = await collection.Apply(x => x.RecordShell.Sales
                * x.RecordShell.SalesAdjustment, "AdjustedSales")
                .AverageAsync(x => x["AdjustedSales"]);
            Assert.Equal(527500, average);

        }
    }
}
