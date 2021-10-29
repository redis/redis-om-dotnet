# Simple Text Queries

The `RedisCollection` provides a fluent interface for querying objects stored in redis. This means that if you store an object in Redis with the Redis OM library, and you have [RediSearch](https://oss.redis.com/redisearch/) enabled, you can query objects stored in Redis with ease using the LINQ syntax you're used to.

## Define the Model

Let's start off by defining a model that we will be using for querying, we will use a `Employee` Class which will have some basic stuff we may want to query in it

```csharp
[Document]
public class Employee
{
    [Indexed]
    public string Name { get; set; }

    [Indexed(Aggregatable = true)]
    public int Age { get; set; }
    
    [Indexed(Aggregatable = true)]
    public double Sales { get; set; }    
    
    [Searchable(Aggregatable = true)]
    public string Department { get; set; }
}
```

## Connect to Redis

Now we will initialize a RedisConnectionProvider, and grab a handle to a RedisCollection for Employee

```csharp
static async Task Main(string[] args)
{
    var provider = new RedisConnectionProvider("redis://localhost:6379");
    var connection = provider.Connection;
    var employees = prover.RedisCollection<Employee>();
    await connection.CreateIndexAsync(typeof(Employee));
}
```

## Create our Index

Next we'll create the index, so next in our `Main` method, let's take our type and condense it into an index

## Seed some Data

Next we'll seed a few piece of data in our database to play around with:

```csharp
var e1 = new Employee {Name = "Bob", Age = 35, Sales = 100000, Department = "EMEA Sales"};
var e2 = new Employee {Name = "Alice", Age = 52, Sales = 300000, Department = "Partner Sales"};
var e3 = new Employee {Name = "Marcus", Age = 42, Sales = 250000, Department = "NA Sales"};
var e4 = new Employee {Name = "Susan", Age = 27, Sales = 200000, Department = "EMEA Sales"};
var e5 = new Employee {Name = "John", Age = 38, Sales = 275000, Department = "APAC Sales"};
var e6 = new Employee {Name = "Theresa", Age = 30, Department = "EMEA Ops"};
var insertTasks = new []
    {
        employees.InsertAsync(e1),
        employees.InsertAsync(e2),
        employees.InsertAsync(e3),
        employees.InsertAsync(e4),
        employees.InsertAsync(e5)
        employees.InsertAsync(e6)
    };
await Task.WhenAll(insertTasks);
```

## Simple Text Query of an Indexed Field

With these data inserted into our database, we can now go ahead and begin querying. Let's start out by trying to query people by name. We can search for all employees named `Susan` with a simple Where predicate:

```csharp
var susans = employees.Where(x => x.Name == "Susan");
await foreach (var susan in susans)
{
    Console.WriteLine($"Susan is {susan.Age} years old and works in the {susan.Department} department ");
}
```

The `Where` Predicates also support `and`/`or` operators, e.g. to find all employees named `Alice` or `Bob` you can use:

```csharp
var AliceOrBobs = employees.Where(x => x.Name == "Alice" || x.Name == "Bob");
await foreach (var employee in AliceOrBobs)
{
    Console.WriteLine($"{employee.Name} is {employee.Age} years old and works in the {employee.Department} Department");
}
```

### Limiting Result Object Fields

When you are querying larger Documents in Redis, you may not want to have to drag back the entire object over the network, in that case you can limit the results to only what you want using a `Select` predicate. E.g. if you only wanted to find out the ages of employees, all you would need to do is select the age of employees:

```csharp
var employeeAges = employees.Select(x => x.Age);
await foreach (var age in employeeAges)
{
    Console.WriteLine($"age: {age}");
}
```

Or if you want to select more than one field you can create a new anonymous object:

```csharp
var employeeAges = employees.Select(x => new {x.Name, x.Age});
await foreach (var e in employeeAges)
{
    Console.WriteLine($"{e.Name} is age: {e.Age} years old");
}
```

### Limiting Returned Objects

You can limit the size of your result (in the number of objects returned) with `Skip` & `Take` predicates. `Skip` will skip over the specified number of records, and `Take` will take only the number of records provided (at most);

```csharp
var people = employees.Skip(1).Take(2);
await foreach (var e in people)
{
    Console.WriteLine($"{e.Name} is age: {e.Age} years old");
}
```

## Full Text Search

There are two types of attributes that can decorate strings, `Indexed`, which we've gone over and `Searchable` which we've yet to discuss. The `Searchable` attribute considers equality slightly differently than Indexed, it operates off a full-text search. In expressions involving Searchable fields, equality—`==`— means a match. A match in the context of a searchable field is not necessarily a full exact match but rather that the string contains the search text. Let's look at some examples.

### Find Employee's in Sales

So we have a `Department` string which is marked as `Searchable` in our Employee class. Notice how we've named our departments. They contain a region and a department type. If we wanted only to find all employee's in `Sales` we could do so with:

```csharp
var salesPeople = employees.Where(x => x.Department == "Sales");
await foreach (var employee in salesPeople)
{
    Console.WriteLine($"{employee.Name} is in the {employee.Department} department");
}
```

This will produce:

```text
Bob is in the EMEA Sales department
Alice is in the Partner Sales department
Marcus is in the NA Sales department
Susan is in the EMEA Sales department
John is in the APAC Sales department
```

Because they are all folks in departments called `sales`

If you wanted to search for everyone in a department in `EMEA` you could search with:

```csharp
var emeaFolks = employees.Where(x => x.Department == "EMEA");
await foreach (var employee in emeaFolks)
{
    Console.WriteLine($"{employee.Name} is in the {employee.Department} department");
}
```

Which of course would produce:

```text
Bob is in the EMEA Sales department
Susan is in the EMEA Sales department
Theresa is in the EMEA Ops department
```

## Sorting

If a `Searchable` or `Indexed` field is marked as `Sortable`, or `Aggregatable`, you can order by that field using `OrderBy` predicates.

```csharp
var employeesBySales = employees.OrderBy(x=>x.Name);
var employeesBySalesDescending = employees.OrderByDescending(x=>x.Name);
```