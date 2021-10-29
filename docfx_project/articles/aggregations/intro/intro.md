# Aggregations Intro

[Aggregations](https://oss.redis.com/redisearch/Aggregations/) are a method of grouping documents together and run processing on them on the server to transform them into data that you need in your application, without having to perform the computation client-side.

## Anatomy of a Pipeline

Aggregations in Redis are build around an aggregation pipeline, you will start off with a `RedisAggregationSet<T>` of objects that you have indexed in Redis. From there you can

* Query to filter down the results you want
* Apply functions to them to combine functions to them
* Group like features together
* Run reductions on groups
* Sort records
* Further filter down records

## Setting up for Aggregations

Redis OM .NET provides an `RedisAggregationSet<T>` class that will let you perform aggregations on employees, let's start off with a trivial aggregation. Let's start off by defining a model:

```csharp
[Document]
public class Employee
{
    [Indexed]
    public string Name { get; set; }
    
    [Indexed]
    public GeoLoc? HomeLoc { get; set; }

    [Indexed(Aggregatable = true)]
    public int Age { get; set; }

    [Indexed(Aggregatable = true)]
    public double Sales { get; set; }
    
    [Indexed(Aggregatable = true)]
    public double SalesAdjustment { get; set; }

    [Searchable(Aggregatable = true)]
    public string Department { get; set; }
}
```

We'll then create the index for that model, pull out a `RedisAggregationSet<T>` from our provider, and initialize the index, and seed some data into our database

```csharp
var provider = new RedisConnectionProvider("redis://localhost:6379");
await provider.Connection.CreateIndexAsync(typeof(Restaurant));
var employees = provider.RedisCollection<Employee>();
var employeeAggregations = provider.AggregationSet<Employee>();
var e1 = new Employee {Name = "Bob", Age = 35, Sales = 100000, SalesAdjustment = 1.5,  Department = "EMEA Sales"};
var e2 = new Employee {Name = "Alice", Age = 52, Sales = 300000, SalesAdjustment = 1.02, Department = "Partner Sales"};
var e3 = new Employee {Name = "Marcus", Age = 42, Sales = 250000, SalesAdjustment = 1.1, Department = "NA Sales"};
var e4 = new Employee {Name = "Susan", Age = 27, Sales = 200000, SalesAdjustment = .95, Department = "EMEA Sales"};
var e5 = new Employee {Name = "John", Age = 38, Sales = 275000, SalesAdjustment = .9, Department = "APAC Sales"};
var e6 = new Employee {Name = "Theresa", Age = 30, Department = "EMEA Ops"};
employees.Insert(e1);
employees.Insert(e2);
employees.Insert(e3);
employees.Insert(e4);
employees.Insert(e5);
employees.Insert(e6);
```

## The AggregationResult

The Aggregations pipeline Is all built around the `RedisAggregationSet<T>` this Set is generic, so you can provide the model that you want to build your aggregations around (an Indexed type), but you will notice that the return type from queries to the `RedisAggregationSet` is the generic type passed into it. Rather it is an `AggregationResult<T>` where `T` is the generic type you passed into it. This is a really important concept, when results are returned from aggregations, they are not hydrated into an object like they are with queries. That's because Aggregations aren't meant to pull out your model data from the database, rather they are meant to pull out aggregated results. The AggregationResult has a `RecordShell` field, which is ALWAYS null outside of the pipeline. It can be used to build expressions for querying objects in Redis, but when the AggregationResult lands, it will not contain a hydrated record, rather it will contain a dictionary of Aggregations built by the Aggregation pipeline. This means that you can access the results of your aggregations by indexing into the AggregationResult.

## Simple Aggregations

Let's try running an aggregation where we find the Sum of the sales for all our employees in EMEA. So the Aggregations Pipeline will use the `RecordShell` object, which is a reference to the generic type of the aggregation set, for something as simple as a group-less SUM, you will simply get back a numeric type from the aggregation.

```csharp
var sumOfSalesEmea = employeeAggregations.Where(x => x.RecordShell.Department == "EMEA")
    .Sum(x => x.RecordShell.Sales);
Console.WriteLine($"EMEA sold:{sumOfSalesEmea}");
```

The `Where` expression tells the aggregation pipeline which records to consider, and subsequently the `SUM` expression indicates which field to sum. Aggregations are a rich feature and this only scratches the surface of it, these pipelines are remarkably flexible and provide you the ability to do all sorts of neat operations on your Data in Redis.
