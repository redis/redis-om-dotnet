using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.RediSearch;
using Redis.OM.RediSearch.Collections;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Collection("Redis")]
    public class AggregationFunctionalTests
    {
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

            _connection.Set(kermit);
            _connection.Set(waldorf);
            _connection.Set(startler);
            _connection.Set(fozzie);
            _connection.Set(beaker);
            _connection.Set(bunsen);
        }

        [Fact]
        public void GetDepartmentBySales()
        {
            Setup();
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
            Setup();
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
        public void GetAdjustedSalesStandardDeviation()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            Task.Run(async () =>
            {
                var stddev = await collection.Apply(x => x.RecordShell.Sales
                    * x.RecordShell.SalesAdjustment, "AdjustedSales")
                    .StandardDeviationAsync(x => x["AdjustedSales"]);
                Assert.Equal(358018.854252, stddev);
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public void GetAverageAdjustedSales()
        {
            Setup();
            var collection = new RedisAggregationSet<Person>(_connection);
            Task.Run(async () =>
            {
                var average = await collection.Apply(x => x.RecordShell.Sales
                    * x.RecordShell.SalesAdjustment, "AdjustedSales")
                    .AverageAsync(x => x["AdjustedSales"]);
                Assert.Equal(527500, average);
            }).GetAwaiter().GetResult();
        }
    }
}
