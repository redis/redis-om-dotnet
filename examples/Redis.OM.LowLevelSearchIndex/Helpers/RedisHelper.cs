using Redis.OM.BasicMatchingQueries.Models;
using Redis.OM.Searching;

namespace Redis.OM.LowLevelSearchIndex.Helpers;

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
        });

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Customer",
            LastName = "3",
            Age = 25,
            IsActive = false,
            Email = "test-3@test.com",
        });

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Testable",
            LastName = "Customer 2",
            Age = 99,
            IsActive = true,
            Email = "test-55@test.com",
        });

        _customerCollection.Insert(new Customer()
        {
            FirstName = "Sharon",
            LastName = "Lim",
            Age = 25,
            IsActive = true,
            Email = "test-111@test.com",
        });
    }

}
