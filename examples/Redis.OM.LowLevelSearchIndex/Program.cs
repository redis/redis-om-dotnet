using Redis.OM;
using Redis.OM.BasicMatchingQueries.Models;
using Redis.OM.LowLevelSearchIndex.Helpers;
using Redis.OM.Searching;
using System.Reflection;

static void ShowCustomers(string type, List<Customer> customers)
{
    Console.WriteLine($"Customer {type}: {string.Join(", ", customers.Select(x => $"{x.FirstName} {x.LastName}"))}, total: {customers.Count}.");
}

var provider = new RedisConnectionProvider("redis://localhost:6379");
provider.Connection.CreateIndex(typeof(Customer));

var redisHelper = new RedisHelper(provider);
redisHelper.InitializeCustomers();

var connection = provider.Connection;

var result = connection.Execute("FT.SEARCH", "customer-idx", "@IsActive:{true} @FirstName|LastName:customer");
var response = new SearchResponse<Customer>(result);

ShowCustomers("Active & First or Last Name have \"customer\"", response.Documents.Values.ToList());

result = connection.Execute("FT.SEARCH", "customer-idx", "(@FirstName|LastName:customer) | (@LastName:customer) => { $weight: 5.0; }");
response = new SearchResponse<Customer>(result);

ShowCustomers("First or Last Name have \"customer\" but prioritize lastname", response.Documents.Values.ToList());

result = connection.Execute("FT.SEARCH", "customer-idx", "customer");
response = new SearchResponse<Customer>(result);

ShowCustomers("All customers with fields text that match with \"customer\"", response.Documents.Values.ToList());