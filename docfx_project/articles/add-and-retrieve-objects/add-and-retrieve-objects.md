# Add and Retrieve Objects

The Redis OM library supports declarative storage and retrieval of objects out of Redis. Without the RediSearch and RedisJson modules, this is limited to using hashes, and id lookups of objects in Redis. You will still use the `Document` Attribute to decorate a class you'd like to store in Redis. From there, all you need to do is either call `Insert` or `InsertAsync` on the `RedisCollection` or `Set` or `SetAsync` on the RedisConnection, passing in the object you want to set in Redis. You can then retrieve those objects with `Get<T>` or `GetAsync<T>` with the `RedisConnection` or with `FindById` or `FindByIdAsync` in the RedisCollection.


```csharp
public class Program
{
    [Document(Prefixes = new []{"Employee"})]
    public class Employee
    {
        [RedisIdField]
        public string Id{ get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public double Sales { get; set; }    

        public string Department { get; set; }
    }
    
    static async Task Main(string[] args)
    {
        var provider = new RedisConnectionProvider("redis://localhost:6379");
        var connection = provider.Connection;
        var employees = provider.RedisCollection<Employee>();
        var employee1 = new Employee{Name="Bob", Age=32, Sales = 100000, Department="Partner Sales"};
        var employee2 = new Employee{Name="Alice", Age=45, Sales = 200000, Department="EMEA Sales"};
        var idp1 = await connection.SetAsync(employee1);
        var idp2 = await employees.InsertAsync(employee2);

        var reconstitutedE1 = await connection.GetAsync<Employee>(idp1);
        var reconstitutedE2 = await employees.FindByIdAsync(idp2);
        Console.WriteLine($"First Employee's name is {reconstitutedE1.Name}, they are {reconstitutedE1.Age} years old, " +
                          $"they work in the {reconstitutedE1.Department} department and have sold {reconstitutedE1.Sales}, " +
                          $"their ID is: {reconstitutedE1.Id}");
        Console.WriteLine($"Second Employee's name is {reconstitutedE2.Name}, they are {reconstitutedE2.Age} years old, " +
                        $"they work in the {reconstitutedE2.Department} department and have sold {reconstitutedE2.Sales}, " +
                        $"their ID is: {reconstitutedE2.Id}");
    }
}
```

The Code above will declare an `Employee` class, and allow you to add employees to Redis, and then retrieve Employees from Redis the output from this method will look like this:


```text
First Employee's name is Bob, they are 32 years old, they work in the Partner Sales department and have sold 100000, their ID is: 01FHDFE115DKRWZW0XNF17V2RK
Second Employee's name is Alice, they are 45 years old, they work in the EMEA Sales department and have sold 200000, their ID is: 01FHDFE11T23K6FCJQNHVEF92F
```

If you wanted to find them in Redis directly you could run `HGETALL Employee:01FHDFE115DKRWZW0XNF17V2RK` and that will retrieve the Employee object as a Hash from Redis. If you do not specify a prefix, the prefix will be the fully-qualified class name.