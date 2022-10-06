using Redis.OM;
using Redis.OM.BasicMatchingQueries.Helpers;
using Redis.OM.BasicMatchingQueries.Models;

static void ShowCustomers(string type, List<Customer> customers)
{
    Console.WriteLine($"Customer {type}: {string.Join(", ", customers.Select(x => $"{x.FirstName} {x.LastName}"))}, total: {customers.Count}.");
}

var provider = new RedisConnectionProvider("redis://localhost:6379");
provider.Connection.CreateIndex(typeof(Customer));

var redisHelper = new RedisHelper(provider);
redisHelper.InitializeCustomers();

var customerCollection = provider.RedisCollection<Customer>();

// match by string
// Find all customers with FirstName is Customer
var customerWithFirstNameCustomer = customerCollection.Where(x => x.FirstName == "Customer").ToList();
ShowCustomers("FirstName == Customer", customerWithFirstNameCustomer);

// match by numeric
// Find all customers with Age is 20
var customerWithAge20 = customerCollection.Where(x => x.Age == 20).ToList();
ShowCustomers("Age == 20", customerWithFirstNameCustomer);

// Find all customers with Age is more than 20
var customerWithAgeMoreThan20 = customerCollection.Where(x => x.Age > 20).ToList();
ShowCustomers("Age > 20", customerWithAgeMoreThan20);

// match by boolean
// Find all customers with IsActive is true
var activeCustomer = customerCollection.Where(x => x.IsActive).ToList();
ShowCustomers("IsActive == true", customerWithFirstNameCustomer);

// match by enums
// Find all customers with Gender is Male
var maleCustomer = customerCollection.Where(x => x.Gender == Gender.Male).ToList();
ShowCustomers("Gender == Gender.Male", customerWithFirstNameCustomer);

// multiple matches

// Find all customers with FirstName is Customer and Age is 20
var customerWithFirstNameCustomerAndAge20 = customerCollection.Where(x => x.FirstName == "Customer" && x.Age == 20).ToList();
ShowCustomers("FirstName == Customer && Age == 20", customerWithFirstNameCustomerAndAge20);

// match by string within embedded documents
// Find all customers with City is Washington
var customerInLondon = customerCollection.Where(x => x.Address.City == "Washington").ToList();
ShowCustomers("Address.City == Washington", customerInLondon);

// Find all customers with City is London and HouseNumber is 100
var customerInLondonWithHouseNumber100 = customerCollection.Where(x => x.Address.City == "London" && x.Address.HouseNumber == 100).ToList();
ShowCustomers("Address.City == London && Address.HouseNumber == 100", customerInLondonWithHouseNumber100);