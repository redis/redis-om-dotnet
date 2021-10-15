using NRedisPlus.RediSearch;
using NRedisPlus.RediSearch.Attributes;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NRedisPlus.Test.ConsoleApp
{
    public class Program
    {
        [Document(StorageType = StorageType.JSON)]
        public class Employee
        {
            [Indexed]
            public string Name { get; set; }

            [Indexed(Aggregatable = true)]
            public int Age { get; set; }
            
            [Indexed(Aggregatable = true)]
            public double Sales { get; set; }
            
            [Indexed(Aggregatable = true)]
            public double SalesAdjustment { get; set; }
            
            [Indexed(Aggregatable = true)]
            public string Department { get; set; }

            [Indexed(Aggregatable = true)] public GeoLoc HomeLoc { get; set; }
            [Indexed(Aggregatable = true)] public GeoLoc OfficeLoc { get; set; }
        }
        
        static async Task Main(string[] args)
        {
            var provider = new RedisConnectionProvider("redis://localhost:6379");

            var connection = provider.Connection;
            var employeeAggregations = provider.AggregationSet<Employee>();

            var averageAge = employeeAggregations.Average(x => x.RecordShell.Age);
            var distanceFromOffice = employeeAggregations.Apply(
                x => ApplyFunctions.GeoDistance(x.RecordShell.HomeLoc, x.RecordShell.OfficeLoc), "DistanceFromOffice");
            foreach (var item in distanceFromOffice)
            {
                Console.WriteLine(item["DistanceFromOffice"].ToString());
            }
            
            
            
            
            
            
            
            var employees = provider.RedisCollection<Employee>();
            try
            {
                await connection.DropIndexAsync(typeof(Employee));
            }
            catch{/*do nothing*/}
            
            await connection.CreateIndexAsync(typeof(Employee));
            
            var rand = new Random();
            var names = new string[] {"Steve", "Jane", "Theresa", "Carl", "Augustin", "Andrew", "Stacy"};
            var departments = new[] {"Partners", "EMEA", "APAC", "NA"};
            var createTasks = new List<Task>();
            for (var i = 0; i < 5000; i++)
            {
                var name = names[rand.Next(names.Length)];
                var age = rand.Next(21, 75);
                var sales = rand.Next(100000, 500000);
                var salesAdjustment = rand.NextDouble()+1;
                var department = departments[rand.Next(departments.Length)];
                var employee = new Employee
                {
                    Age = age,
                    Name = name,
                    Department = department,
                    Sales = sales,
                    SalesAdjustment = salesAdjustment
                };
                createTasks.Add(connection.SetAsync(employee));
            }
            const string ADJUSTED_SALES = "AdjustedSales";
            const string ADJUSTED_SALES_SUM = "AdjustedSales_SUM";
            var averageSales = employeeAggregations.Average(x => x.RecordShell.Sales);
            var averageAdjustedSales = employeeAggregations
                .Apply(x => x.RecordShell.Sales * x.RecordShell.SalesAdjustment, ADJUSTED_SALES)
                .Average(x=>x[ADJUSTED_SALES]);
            Console.WriteLine($"Average Sales:{averageSales}");
            Console.WriteLine($"Adjusted Sales Average:{averageAdjustedSales}");

            var departmentsRankedByAdjustedSales = employeeAggregations
                .Apply(x => x.RecordShell.Sales * x.RecordShell.SalesAdjustment, ADJUSTED_SALES)
                .GroupBy(x => x.RecordShell.Department)
                .Sum(x => x[ADJUSTED_SALES])
                .OrderBy(x => x[ADJUSTED_SALES_SUM]);
            await foreach(var dep in departmentsRankedByAdjustedSales)
            {                
                Console.WriteLine($"The {dep[nameof(Employee.Department)]} department sold: {dep[ADJUSTED_SALES_SUM]}");
            }            
        }
    }
}
