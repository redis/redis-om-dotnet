# Numeric Queries

In addition to providing capabilities for text queries, Redis OM also provides you the ability to perform numeric equality and numeric range queries. Let us assume a model of:

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

Assume that we've connected to Redis already and retrieved a `RedisCollection` and seeded some data as such:

```csharp
var employees = provider.RedisCollection<Employee>();
var e1 = new Employee {Name = "Bob", Age = 35, Sales = 100000, Department = "EMEA Sales"};
var e2 = new Employee {Name = "Alice", Age = 52, Sales = 300000, Department = "Partner Sales"};
var e3 = new Employee {Name = "Marcus", Age = 42, Sales = 250000, Department = "NA Sales"};
var e4 = new Employee {Name = "Susan", Age = 27, Sales = 200000, Department = "EMEA Sales"};
var e5 = new Employee {Name = "John", Age = 38, Sales = 275000, Department = "APAC Sales"};
var e6 = new Employee {Name = "Theresa", Age = 30, Department = "EMEA Ops"};
employees.Insert(e1);
employees.Insert(e2);
employees.Insert(e3);
employees.Insert(e4);
employees.Insert(e5);
employees.Insert(e6);
```

We can now perform queries against the numeric values in our data as you would with any other collection using LINQ expressions.

```csharp
var underThirty = employees.Where(x=>x.Age < 30);
var middleTierSales = employees.Where(x=>x.Sales > 100000 && x.Sales < 300000);
```

You can of course also pair numeric queries with Text Queries:

```csharp
var emeaMidTier = employees.Where(x=>x.Sales>100000 & x.Sales <300000 && x.Department == "EMEA");
```

## Sorting

If an `Indexed` field is marked as `Sortable`, or `Aggregatable`, you can order by that field using `OrderBy` predicates.

```csharp
var employeesBySales = employees.OrderBy(x=>x.Sales);
var employeesBySalesDescending = employees.OrderByDescending(x=>x.Sales);
```