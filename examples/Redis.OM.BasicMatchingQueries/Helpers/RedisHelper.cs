using Redis.OM.BasicMatchingQueries.Models;
using Redis.OM.Searching;

namespace Redis.OM.BasicMatchingQueries.Helpers;

public class RedisHelper
{
    private readonly IRedisCollection<Customer> _customerCollection;

    public RedisHelper(RedisConnectionProvider provider)
    {
        _customerCollection = provider.RedisCollection<Customer>();
    }

    public void InitializeCustomers()
    {
        var count = _customerCollection.Count();
        if (count > 0)
        {
            // not re-add when already initialize
            return;
        }

        Console.WriteLine("Initialize Customer Data...");

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Customer",
            LastName = "2",
            Age = 20,
            IsActive = true,
            Email = "test@test.com",
            Gender = Gender.Male,
            Address = new Address()
            {
                City = "London",
                HouseNumber = 99,
            }
        });

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Customer",
            LastName = "3",
            Age = 25,
            IsActive = false,
            Email = "test-3@test.com",
            Gender = Gender.Female,
            Address = new Address()
            {
                City = "London",
                HouseNumber = 100,
            }
        });

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Testable",
            LastName = "2",
            Age = 99,
            IsActive = true,
            Email = "test-55@test.com",
            Gender = Gender.Other,
            Address = new Address()
            {
                City = "Washington",
                HouseNumber = 99,
            }
        });

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Sharon",
            LastName = "Lim",
            Age = 25,
            IsActive = true,
            Email = "test-111@test.com",
            Gender = Gender.Male,
            Address = new Address()
            {
                City = "London",
                HouseNumber = 1000,
            }
        });
    }

}
