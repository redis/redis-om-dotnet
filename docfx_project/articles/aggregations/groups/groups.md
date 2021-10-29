# Grouping and Reduction

Grouping and reducing operations using aggregations can be extremely powerful.

## What Is a Group

A group is simply a group of like records in Redis.

e.g. 

```json
{
    "Name":"Susan",
    "Department":"Sales",
    "Sales":600000
}

{
    "Name":"Tom",
    "Department":"Sales",
    "Sales":500000
}
```

If grouped together by `Department` would be one group. When grouped by `Name`, they would be two groups.

## Reductions

What makes groups so useful in Redis Aggregations is that you can run reductions on them to aggregate items within the group. For example, you can calculate summary statistics on numeric fields, retrieve random samples, distinct counts, approximate distinct counts of any aggregatable field in the set.

## Using Groups and Reductions with Redis OM .NET

You can run reductions against an `RedisAggregationSet` either with or without a group. If you run a reduction without a group, the result of the reduction will materialize immediately as the desired type. If you run a reduction against a group, the results will materialize when they are enumerated.

### Reductions without a Group

If you wanted to calculate a reduction on all the records indexed by Redis in the collection, you would simply call the reduction on the `RedisAggregationSet`

```csharp
var sumSales = employeeAggregations.Sum(x=>x.RecordShell.Sales);
Console.WriteLine($"The sum of sales for all employees was {sumSales}");
```

### Reductions with a Group

If you want to build a group to run reductions on, e.g. you wanted to calculate the average sales in a department, you would use a `GroupBy` predicate to specify which field or fields to group by. If you want to group by 1 field, your lambda function for the group by will yield just the field you want to group by. If you want to group by multiple fields, `new` up an anonymous type in line:

```csharp
var oneFieldGroup = employeeAggregations.GroupBy(x=>x.RecordShell.Department);

var multiFieldGroup = employeeAggregations.GroupBy(x=>new {x.RecordShell.Department, x.RecordShell.WorkLoc});
```

From here you can run reductions on your groups. To run a Reduction, execute a reduction function. When the collection materializes the `AggregationResult<T>` will have the reduction stored in a formatted string which is the `PropertyName_COMMAND_POSTFIX`, see supported operations table below for postfixes. If you wanted to calculate the sum of the sales of all the departments you could:

```csharp
var departments = employeeAggregations.GroupBy(x=>x.RecordShell.Department).Sum(x=>x.RecordShell.Sales);
foreach(var department in departments)
{
    Console.WriteLine($"The {department[nameof(Employee.Department)]} department sold {department["Sales_SUM"]}");
}
```

|Command Name|Command Postfix|Description|
|------------|----------------|-----------|
|Count|COUNT|number of records meeting the query, or in the group|
|CountDistinct|COUNT_DISTINCT|Counts the distinct occurrences of a given property in a group|
|CountDistinctish|COUNT_DISTINCTISH|Provides an approximate count of distinct occurrences of a given property in each group - less expensive computationally but does have a small 3% error rate |
|Sum|SUM|The sum of all occurrences of the provided field in each group|b
|Min|MIN|Minimum occurrence for the provided field in each group|
|Max|MAX|Maximum occurrence for the provided field in each group|
|Average|Avg|Arithmetic mean of all the occurrences for the provided field in a group|
|StandardDeviation|STDDEV|Standard deviation from the arithmetic mean of all the occurrences for the provided field in each group|
|Quantile|QUANTLE|The value of a record at the provided quantile for a field in each group, e.g., the Median of the field would be sitting at quantile .5|
|Distinct|TOLIST|Enumerates all the distinct values of a given field in each group|
|FirstValue|FIRST_VALUE|Retrieves the first occurrence of a given field in each group|
|RandomSample|RANDOM_SAMPLE_{NumRecords}|Random sample of the given field in each group|

## Closing Groups

When you invoke a `GroupBy` the type of return type changes from `RedisAggregationSet` to a `GroupedAggregationSet`. In some instances you may need to close a group out and use its results further down the pipeline. To do this, all you need to do is call `CloseGroup` on the `GroupedAggregationSet` - that will end the group predicates and allow you to use the results further down the pipeline.