# Apply Functions

Apply functions are functions that you can define as expressions to apply to your data in Redis. In essence, they allow you to combine your data together, and extract the information you want.

## Data Model

For the remainder of this article we will be using this data model:

```csharp
[Document]
public class Employee
{
    [Indexed(Aggregatable = true)]
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

    [Indexed(Aggregatable = true)] 
    public long LastOnline { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
```

## Anatomy of an Apply Function

`Apply` is a method on the `RedisAggregationSet<T>` class which takes two arguments, each of which is a component of the apply function. 

First it takes the expression that you want Redis to execute on every record in the pipeline, this expression takes a single parameter, an `AggregationResult<T>`, where `T` is the generic type of your `RedisAggregationSet`. This AggregationResult has two things we should think about, first it contains a `RecordShell` which is a placeholder for the generic type, and secondly it has an `Aggregations` property - which is a dictionary containing the results from your pipeline. Both of these can be used in apply functions. 

The second component is the alias, that's the name the result of the function is stored in when the pipeline executes.

### Adjusted Sales

Our data model has two properties related to sales, `Sales`, how much the employee has sold, and `SalesAdjustment`, a figure used to adjust sales based off various factors, perhaps territory covered, experience, etc. . . The idea being that perhaps a fair way to analyze an employee's performance is a combination of these two fields rather than each individually. So let's say we wanted to find what everyone's adjusted sales were, we could do that by creating an apply function to calculate it.

```csharp
var adjustedSales = employeeAggregations.Apply(x => x.RecordShell.SalesAdjustment * x.RecordShell.Sales,
    "ADJUSTED_SALES");
foreach (var result in adjustedSales)
{
    Console.WriteLine($"Adjusted Sales were: {result["ADJUSTED_SALES"]}");
}
```

## Arithmetic Apply Functions

Functions that use arithmetic and math can use the mathematical operators `+` for addition, `-` for subtraction, `*` for multiplication, `/` for division, and `%` for modular division, also the `^` operator, which is typically used for bitiwise exclusive-or operations, has been reserved for power functions. Additionally, you can use many `System.Math` library operations within Apply functions, and those will be translated to the appropriate methods for use by Redis.

### Available Math Functions

|Function|Type|Description|Example|
|--------|----|-----------|-------|
|Log10|Math|yields the 10 base log for the number|`Math.Log10(x["AdjustedSales"])`|
|Abs|Math|yields the absolute value of the provided number|`Math.Abs(x["AdjustedSales"])`|
|Ceil|Math|yields the smallest integer not less than the provided number|`Math.Ceil(x["AdjustedSales"])`|
|Floor|Math|yields the smallest integer not greater than the provided number|`Math.Floor(x["AdjustedSales"])`|
|Log|Math|yields the Log base 2 for the provided number|`Math.Log(x["AdjustedSales"])`|
|Exp|Math|yields the natural exponent for the provided number (e^y)|`Math.Exp(x["AdjustedSales"])`|
|Sqrt|Math|yields the Square root for the provided number|`Math.Sqrt(x["AdjustedSales"])`|

## String Functions

You can also apply multiple string functions to your data, if for example you wanted to create a birthday message for each employee you could do so by calling `String.Format` on your records:

```csharp
var birthdayMessages = employeeAggregations.Apply(x =>
    string.Format("Congratulations {0} you are {1} years old!", x.RecordShell.Name, x.RecordShell.Age), "message");
await foreach (var message in birthdayMessages)
{
    Console.WriteLine(message["message"].ToString());
}
```

### List of String Functions:

|Function|Type|Description|Example|
|--------|----|-----------|-------|
|ToUpper|String|yields the provided string to upper case|`x.RecordShell.Name.ToUpper()`|
|ToLower|String|yields the provided string to lower case|`x.RecordShell.Name.ToLower()`|
|StartsWith|String|Boolean expression - yields 1 if the string starts with the argument|`x.RecordShell.Name.StartsWith("bob")`|
|Contains|String|Boolean expression - yields 1 if the string contains the argument |`x.RecordShell.Name.Contains("bob")`|
|Substring|String|yields the substring starting at the given 0 based index, the length of the second argument, if the second argument is not provided, it will simply return the balance of the string|`x.RecordShell.Name.Substring(4, 10)`|
|Format|string|Formats the string based off the provided pattern|`string.Format("Hello {0} You are {1} years old", x.RecordShell.Name, x.RecordShell.Age)`|
|Split|string|Split's the string with the provided string - unfortunately if you are only passing in a single splitter, because of how expressions work, you'll need to provide string split options so that no optional parameters exist when building the expression, just pass `StringSplitOptions.None`|`x.RecordShell.Name.Split(",", StringSplitOptions.None)`|

## Time Functions

You can also perform functions on time data in Redis. If you have a timestamp stored in a useable format, a unix timestamp or a timestamp string that can be translated from [strftime](http://strftime.org/), you can operate on them. For example if you wanted to translate a unix timestamp to YYYY-MM-DDTHH:MM::SSZ you can do so by just calling `ApplyFunctions.FormatTimestamp` on the record inside of your apply function. E.g.

```csharp
var lastOnline = employeeAggregations.Apply(x => ApplyFunctions.FormatTimestamp(x.RecordShell.LastOnline),
    "LAST_ONLINE_STRING");

foreach (var employee in lastOnline)
{
    Console.WriteLine(employee["LAST_ONLINE_STRING"].ToString());
}
```

### Time Functions Available

|Function|Type|Description|Example|
|--------|----|-----------|-------|
|ApplyFunctions.FormatTimestamp|time|transforms a unix timestamp to a formatted time string based off [strftime](http://strftime.org/) conventions|`ApplyFunctions.FormatTimestamp(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.ParseTime|time|Parsers the provided formatted timestamp to a unix timestamp|`ApplyFunctions.ParseTime(x.RecordShell.TimeString, "%FT%ZT")`|
|ApplyFunctions.Day|time|Rounds a unix timestamp to the beginning of the day|`ApplyFunctions.Day(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.Hour|time|Rounds a unix timestamp to the beginning of current hour|`ApplyFunctions.Hour(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.Minute|time|Round a unix timestamp to the beginning of the current minute|`ApplyFunctions.Minute(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.Month|time|Rounds a unix timestamp to the beginning of the current month|`ApplyFunctions.Month(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.DayOfWeek|time|Converts the unix timestamp to the day number with Sunday being 0|`ApplyFunctions.DayOfWeek(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.DayOfMonth|time|Converts the unix timestamp to the current day of the month (1..31)|`ApplyFunctions.DayOfMonth(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.DayOfYear|time|Converts the unix timestamp to the current day of the year (1..31)|`ApplyFunctions.DayOfYear(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.Year|time|Converts the unix timestamp to the current year|`ApplyFunctions.Year(x.RecordShell.LastTimeOnline)`|
|ApplyFunctions.MonthOfYear|time|Converts the unix timestamp to the current month (0..11)|`ApplyFunctions.MonthOfYear(x.RecordShell.LastTimeOnline)`|

## Geo Distance

Another useful function is the `GeoDistance` function, which allows you computer the distance between two points, e.g. if you wanted to see how far away from the office each employee was you could use the `ApplyFunctions.GeoDistance` function inside your pipeline:

```csharp
var officeLoc = new GeoLoc(-122.064181, 37.377207);
var distanceFromWork =
    employeeAggregations.Apply(x => ApplyFunctions.GeoDistance(x.RecordShell.HomeLoc, officeLoc), "DistanceToWork");
await foreach (var element in distancesFromWork)
{
    Console.WriteLine(element["DistanceToWork"].ToString());
}
```