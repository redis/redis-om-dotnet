using Newtonsoft.Json.Linq;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Net;

var connection = new RedisConnectionProvider(ConfigurationOptions.Parse("localhost:6379"));
var customers = connection.RedisCollection<Customer>();
connection.Connection.CreateIndex(typeof(Customer));

// Insert customer
customers.Insert(new Customer()
{
    FirstName = "James",
    LastName = "Bond"
});

// Find all customers with the nickname of Jim
var test = await customers.Where(x => x.FirstName.Contains("James")).Select(x => new test() { FirstName = x.FirstName, LastName = x.LastName }).ToListAsync();

Console.ReadLine();

[Document(StorageType = StorageType.Json)]
public class Customer
{
    [Indexed] public string FirstName { get; set; }
    [Indexed] public string LastName { get; set; }
}

public class test
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}