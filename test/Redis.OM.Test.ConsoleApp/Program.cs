using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM;
using Redis.OM.Modeling;

namespace Redis.OM.Test.ConsoleApp
{
    public class Program
    {
        [Document(StorageType = StorageType.Json)]
        public class Customer
        {
            [Indexed] public string FirstName { get; set; }
            [Indexed] public string LastName { get; set; }
            [Indexed] public string Email { get; set; }
            [Indexed(Aggregatable = true)] public int Age { get; set; }
            [Indexed(Aggregatable = true)] public GeoLoc Home { get; set; }
        }

        private static void Main(string[] args)
        {
            // connect
            var provider = new RedisConnectionProvider("redis://localhost:6379");
            var connection = provider.Connection;
            var customers = provider.RedisCollection<Customer>();
            var customerAggregations = provider.AggregationSet<Customer>();

            // Create index
            connection.CreateIndex(typeof(Customer));

            // Get Index info
            var indexinfo = connection.GetIndexInfo(typeof(Customer));

            // Insert Object
            customers.Insert(new Customer
            {
                FirstName = "James",
                LastName = "Bond",
                Email = "bondjamesbond@email.com",
                Age = 68
            });

            // query
            //// Find all customers who's last name is "Bond"
            //var res = customers.Where(x => x.LastName.Contains("Bond")).ToList();
            //var res2 = customers.Where(x => x.LastName.Contains("*Bon*")).ToList();
            var res3 = customers.Where(x => x.LastName.EndsWith("Bo*")).ToList();

            // Find all customers who's last name is Bond OR who's age is greater than 65
            customers.Where(x => x.LastName == "Bond" || x.Age > 65);

            // Find all customer's who's last name is Bond AND who's first name is James
            customers.Where(x => x.LastName == "Bond" && x.FirstName == "James");

            // Get Average Age
            customerAggregations.Average(x => x.RecordShell.Age);

            // Format Customer Full Names
            customerAggregations.Apply(x => string.Format("{0} {1}", x.RecordShell.FirstName, x.RecordShell.LastName),
                "FullName");

            // Get Customer Distance from Mall of America.
            customerAggregations.Apply(x => ApplyFunctions.GeoDistance(x.RecordShell.Home, -93.241786, 44.853816),
                "DistanceToMall");
        }
    }
}