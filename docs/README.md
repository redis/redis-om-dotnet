# Redis OM

Welcome to Redis OM, Redis' high-level abstraction above Redis for .NET. The library's aim is the singular goal of making Redis more accessible to .NET developers.

## Table of Contents

* [Redis Developer Dotnet](#redis-developer-dotnet)
   * [Project Goals](#project-goals)
      * [Basic Redis Operations with OSS Redis &amp; RedisJson](#basic-redis-operations-with-oss-redis--redisjson)
      * [RediSearch &amp; RedisJson](#redisearch--redisjson)
   * [Getting Started](#getting-started)
      * [Installation](#installation)
      * [Initialize Client - Basic Usage](#initialize-client---basic-usage)
         * [Basic Set &amp; Get](#basic-set--get)
         * [Command Usage](#command-usage)
      * [Initialize Client ASP.NET Core](#initialize-client-aspnet-core)
   * [Querying With the RedisCollection](#querying-with-the-rediscollection)
      * [Defining Indices](#defining-indices)
         * [Document Attribute](#document-attribute)
         * [Property Attributes](#property-attributes)
            * [RedisIdField](#redisidfield)
            * [Indexed Fields](#indexed-fields)
         * [Example Class](#example-class)
      * [Creating Indices](#creating-indices)
         * [Create](#create)
         * [Migrate](#migrate)
   * [Querying](#querying)
      * [Basic Queries](#basic-queries)
         * [Text Matching](#text-matching)
         * [Numeric Range Matching](#numeric-range-matching)
         * [Geo Radius Filtering](#geo-radius-filtering)
         * [Limiting Search Results:](#limiting-search-results)
         * [Offsetting Into Search Results](#offsetting-into-search-results)
         * [Retrieve Only Select Fields](#retrieve-only-select-fields)
   * [Aggregations](#aggregations)
      * [Anatomy of the Pipeline](#anatomy-of-the-pipeline)
         * [The AggregationResult](#the-aggregationresult)
      * [Groups](#groups)
         * [Single Field Groups](#single-field-groups)
         * [Multi-Field Groups](#multi-field-groups)
         * [Closing a Group](#closing-a-group)
      * [Reducers](#reducers)
         * [Immediate Reducer Materialization Example](#immediate-reducer-materialization-example)
         * [Grouped Reductions](#grouped-reductions)
         * [Running Multiple Reductions Without Grouping](#running-multiple-reductions-without-grouping)
         * [Accessing the Results of Reductions](#accessing-the-results-of-reductions)
      * [Apply Expressions](#apply-expressions)
         * [Supported Apply Functions:](#supported-apply-functions)
   * [FAQ](#faq)

## Project Goals

The core goal of this project is to make developers' interactions with Redis simpler by distilling what can be highly complex data modeling. The responsibilities of this library will expand over time, but as of this writing, the core goals are as follows:

### Basic Redis Operations with OSS Redis & RedisJson

* Provide a declarative means for defining how you will store your data in Redis
* Provide an easy interface for storing objects in Redis
* Provide an easy interface for retrieving stored objects from Redis

### RediSearch & RedisJson

* Provide a declarative means for defining how you index your in Redis
* Provide an intuitive, fluent interface for searching through documents stored in Redis

## Getting Started

### Installation

There are few options for installing the library:

1. Using the dotnet CLI, you can run `dotnet install package Redis.Developer.Dotnet`
2. Using the Package Manager CLI you can run `Install-Package Redis.Developer.Dotnet`
3. You can add the package as a Package Reference in your `csproj` file using: `<PackageReference Include="Redis.Developer.Dotnet" Version="0.1.0" />`

### Initialize Client - Basic Usage

Usage of this library revolves around three interfaces:

1. `RedisCollection`
   * Provides Fluent API for querying your data
   * Build's and Migrates secondary indices
   * Provides for the setting and getting of objects in Redis
2. `RedisAggregationSet`
   * Provides Fluent API for building Aggregation pipelines against your data stored in Redis
3. `IRedisConnection`
   * Provides Command level interface for speaking to Redis

To use the features of this library, you'll need to create a `RedisConnectionProvider` object; you'll initialize it with either a connection string or a `RedisConnectionConfiguration` object. From there, you can use the `RedisCollection` method, the `AggregationSet` method, and the `Connection` property to get those objects.

```csharp
var provider = new RedisConnectionProvider("redis://localhost:6379");
var connection = provider.Connection;
var collection = provider.RedisCollection<Person>();
var aggregations = provider.AggregationSet<Person>();
```

#### Configuration URI

Initalizing the RedisConnectionProvider takes a specialized 

With those in hand, you can now run [queries](https://oss.redis.com/redisearch/Commands/#ftsearch), build [Aggregation Pipelines](https://oss.redis.com/redisearch/Aggregations/), save objects to redis, get objects out of redis by ID, and execute arbitrary commands [commands](https://redis.io/commands) against Redis using.

#### Basic Set & Get

To perform basic Get & Set operations on your data.

```csharp
var id = collection.Add(new Person{Name = "Kermit the Frog"});
var kermit = collection.FindById(id);
```

#### Command Usage

```csharp
connection.Execute("SET", "foo", "bar");
var foo = connection.Execute("GET", "foo");
Console.WriteLine(foo);
```

```csharp
await connection.ExecuteAsync("SET", "foo", "bar");
var foo = await connection.ExecuteAsync("GET", "foo");
Console.WriteLine(foo);
```

### Initialize Client ASP.NET Core

In ASP.NET Core, dependency injects the client into your application. In your Startup.cs file's `ConfigureServices` method add the `RedisConnectionProvider` as a singleton Service.


```csharp
public void ConfigureServices(IServiceCollection services)
{
    var provider = new RedisConnectionProvider("localhost:6379");
    services.AddSingleton(provider);
}
```

Then in your controllers and other services, you can dependency inject the provider and pull out the collections and connections you'll need.

```csharp
[ApiController]
[Route("/api/foo")]
public class FooController : Controller
{
    private readonly RedisCollection<Person> _peopleCollection;
    private readonly RedisAggregationSet<Person> _peopleAggregations;
    private readonly IRedisConnection _connection;

    public FooController(RedisConnectionProvider provider)
    {
        _peopleCollection = provider.RedisCollection<Person>();
        _peopleAggregations = provider.AggregationSet<Person>();
        _connection = provider.Connection;
    }
}
```

## Querying With the RedisCollection

The RedisCollection is an `IQueryable` that provides a variety of services:
It allows simple serialization/deserialization services or objects sent to & from Redis.
It also provides a straightforward declarative interface for building [secondary indices](https://oss.redis.com/redisearch/Commands/#ftcreate).
It provides a Fluent API for querying objects that you've stored in Redis.

### Defining Indices
Before you can query anything, you'll have to create an index; to do so, you'll decorate your classes with the attributes defining declaring your index. For example, let's pretend we have a `Person` class that we want to index. First, you can decorate the class itself with metadata for the classes index using the `Document`Attribute, and then you can decorate each property with an appropriate `Index` attribute.

#### Document Attribute

We use the `Document` Attribute to decorate a whole class and define the top-level index. It has the following options:

|Property Name|Description|Default|Optional|
|-------------|-----------|-------|--------|
|StorageType|Defines the underlying data structure used to store the object in Redis, options are `HASH` and `JSON`, Note JSON is only useable with the [RedisJson module](https://oss.redis.com/redisjson/)|HASH|true|
|IndexName|The name of the index |`$"{ClassName.ToLower()}-idx}`|true|
|Prefixes|The key prefixes for redis to build an index off of |`new string[]{$"{ClassName}:"}`|true|
|IdGenerationStrategy|The strategy used to generate Ids for documents, if left blank it will use a [ULID](https://github.com/ulid/spec) generation strategy|UlidGenerationStrategy|true|
|Language| Language to use for full-text search indexing|`null`|true|
|LanguageField|The name of the field in which the document stores its Language|null|true|
|Filter|The filter to use to determine whether a particular item is indexed, e.g. `@Age>=18` |null|true|

#### Property Attributes

##### RedisIdField

Every class indexed by Redis must contain an Id Field marked with the `RedisIdField` - where the Id is stored.

##### Indexed Fields

In addition to declaring an Id Field, you can also declare indexed fields, which will let you search for values within those fields afterward. There are two types of Field level attributes.

1. Indexed - The exact way that the indexed field is interpreted depends on the index type.  It can be applied to fields with the following value types.
	* `string`
	* Numeric types such as `double`, `int`, `float`, etc.
	* `GeoLoc`
	* Array of `string` or `bool`
	* List of `string` or `bool`

2. Searchable - This enables full-text search on the decorated field.  It can be applied to fields with the following value types.
	* `string`

###### IndexedAttribute Properties

There are properties inside the `IndexedAttribute` that let you further customize how things are stored & queried.

|PropertyName|type|Description|Default|Optional|
|------------|----|-----------|-------|--------|
|PropertyName|`string`|The name of the property to be indexed|The name of the property being indexed|true|
|Sortable|`bool`|Whether to index the item so it can be sorted on in queries, enables use of `OrderBy` & `OrderByDescending` -> `collection.OrderBy(x=>x.Email)`|`false`|true|
|Normalize|`bool`|Only applicable for `string` type fields Determines whether the text in a field is normalized (sent to lower case) for purposes of sorting|`true`|true|
|Separator|`char`|Only applicable for `string` type fields Character to use for separating tag field, allows the application of multiple tags fo the same item e.g. `article.Category = technology,parenting` is delineated by a `,` means that `collection.Where(x=>x.Category == "technology")` and `collection.Where(x=>x.Category == "parenting")` will both match the record|`|`|true|
|CaseSensitive|`bool`|Only applicable for `string` type fields - Determines whether case is considered when performing matches on tags|`false`|true|

###### SearchableAttribute Properties

There are properties for the `SearchableAttribute` that let you further customize how the full-text search determines matches

|PropertyName|type|Description|Default|Optional|
|------------|----|-----------|-------|--------|
|PropertyName|`string`|The name of the property to be indexed|The name of the indexed property |true|
|Sortable|`bool`|Whether to index the item so it can be sorted on in queries, enables use of `OrderBy` & `OrderByDescending` -> `collection.OrderBy(x=>x.Email)`|`false`|true|
|NoStem|`bool`|Determines whether to use [stemming](https://oss.redis.com/redisearch/Stemming/), in other words adding the stem of the word to the index, setting to true will stop the Redis from indexing the stems of words|`false`|true|
|PhoneticMatcher|`string`|The phonetic matcher to use if you'd like the index to use (PhoneticMatching)[https://oss.redis.com/redisearch/Phonetic_Matching/] with the index|null|true|
|Weight|`double`|determines the importance of the field for checking result accuracy|1.0|true|

#### Example Class

```csharp
[Document]
public partial class Person
{
    [RedisIdField]
    public string Id { get; set; }    

    [Searchable(Sortable = true)]        
    public string Name { get; set; }

    [Indexed(Aggregatable = true)]
    public GeoLoc? Home { get; set; }

    [Indexed(Aggregatable = true)]
    public GeoLoc? Work { get; set; }

    [Indexed(Sortable = true)]
    public int? Age { get; set; }

    [Indexed(Sortable = true)]
    public int? DepartmentNumber { get; set; }

    [Indexed(Sortable = true)]
    public double? Sales { get; set; }

    [Indexed(Sortable = true)]
    public double? SalesAdjustment { get; set; }

    [Indexed(Sortable = true)]
    public long? LastTimeOnline { get; set; }
    
    [Indexed(Aggregatable = true)]
    public string Email { get; set; }
}
```

### Creating Indices

With the Index defined, the next step is to create the index. There are two methods in `IRedisConnection` we can use,

1. `IRedisConnection.CreateIndex(Type type)`
2. `IRedisConnection.MigrateIndex(Type type)`

`CreateIndex` will create the index described by the `Type` parameter (which will be disassembled and serialized into a redis index) if and only if the Index does not already exist in Redis. `MigrateIndex`, on the other hand, will look at the index already in Redis and add/remove what is needed to bring it into compliance with the prescribed index.

#### Create

```csharp
var connection = RedisConnectionProvider.Connection;
connection.CreateIndex(typeof(Person));
```

#### Migrate

```csharp
var connection = RedisConnectionProvider.Connection;
connection.MigrateIndex(typeof(Person));
```

## Querying

Redis Developer Dotnet provides a Fluent API to build queries. The RedisCollection is an extension of `IQueryable` so you can apply typical lambda expressions you'd expect to use elsewhere. Only when you enumerate the collection is the query built and executed against redis.

### Basic Queries

You can use a `Where` expression to run a query for items matching the expression. For now, the first `Where` expression will be the only one honored. Use `Where` expressions to look for: exact text matches for `TAG` fields, full-text searches for `TEXT` fields, equality expressions for `NUMERIC` fields, and `Contains` checks for arrays of tag fields, and `GeoFilter` queries for `GEO` fields.

#### Text Matching

```csharp
var steves = _peopleCollection
    .Where(x=>x.FirstName == "Steve");
```

#### Numeric Range Matching

```csharp
var adults = _peopleCollection
    .Where(x=>x.Age >= 18);
```

#### Geo Radius Filtering

```csharp
var walkable = _people.Collection
    .GeoFilter(x=>x.Home, 33.7756, 84.3963, 2, GeoLocDistanceUnit.Kilometers);
```

#### Limiting Search Results:

```csharp
var adults = _peopleCollection
    .Where(x=>x.Age >= 18)
    .Take(10);
```

#### Offsetting Into Search Results

```csharp
var adults = _peopleCollection
    .Where(x=>x.Age >= 18)
    .Skip(5);
```

#### Retrieve Only Select Fields

```csharp
var adultNames = _peopleCollection
    .Where(x=>x.Age>=18)
    .Select(x=>x.Name);
```

```csharp
var adults = _peopleCollection
    .Where(x=>x.Age>=18)
    .Select(x=>new {x.Name x.Height});
```

## Aggregations

Another powerful feature provided by Redis Developer Dotnet is how it handles [aggregation pipelines](https://oss.redis.com/redisearch/Aggregations/). Aggregations are a way to process the results of a query, transform them, and extract the data you need, all on the Redis Side. Like the RedisCollection, the RedisAggregationSet allows you to build your Aggregations with expressions; then, when the aggregation is either enumerated or reduced to a single value, the aggregation is built and run against Redis.

### Anatomy of the Pipeline

You build aggregation pipelines with a chain of Aggregation expressions. Every expression will return one of two types.

1. A collection of AggregationResult<T> where `T` is a type indexed with Redis
2. A single collated record, for example, a count, average, standard deviation, etc...

#### The AggregationResult

The `AggregationResult<T>` contains two properties that are relevant to using aggregations.

1. The `RecordShell` property is a remote record shell of the indexed type. You should only use`RecordShell` inside of expressions in an Aggregation pipeline; it will never hold a real value inside the runtime. This absence is because an Aggregation does not load all the data out of the indexed item.
2. `Aggregations` property - This is where all results of an Aggregation are stored once the Results are hydrated. It's a dictionary, and the item's in it can be accessed by looking up their key name.

### Groups

You can group aggregations by different properties by using Grouping Aggregations. If, for example, you wanted to group all your people by DepartmentNumber, you should use a `GroupBy` predicate.

#### Single Field Groups

```csharp
var departments = _peopleCollection.GroupBy(x=>x.RecordShell.DepartmentNumber);
```

#### Multi-Field Groups

To group by multiple fields, there are two options.

1. Specify an anonymous type to group by, that will pull out the property names from the anonymous type and use those to build the `GroupBy` predicate
```csharp
var res = collection
   .GroupBy(x => new {x.RecordShell.Name, AggAge = x["AggAge"]});
```
2. You can chain `GroupBy` predicates together to build them into the pipeline
```csharp
var res = collection
   .GroupBy(x => x.RecordShell.Name)
   .GroupBy(x => x.RecordShell.Age);
```

#### Closing a Group

Suppose you need to close a group predicate (for example, you would like to use the reductions produced by the group further down the pipeline). Then, you can use `CloseGroup`. In that case, this will end the group predicate and allow you to use the aggregation otherwise normally.

### Reducers

Reducers collate the members of a group together to a single output (count, count_distinct, sum, average, standard deviation, etc. . .). To execute them, you'll call the corresponding function, passing in a reduction expression, with the property to reduce. If you have an open GroupBy predicate, Reducers will chain together. If not, the value will immediately materialize (execute on the server), and the value requested will be available immediately. If you would like to run multiple reducers with no group, call GroupBy with an empty anonymous type.

#### Immediate Reducer Materialization Example

If you wanted to, for example, find the sum of all employee sales, you could do so by just calling `Sum` on their sales.

```csharp
var sumSales = colleciton.Sum(x=>x.RecordShell.Sales);
```

#### Grouped Reductions

If you wanted to, for example, find the sum of all your employee-adjusted sales by department, you can by applying the adjusted sales algorithm, grouping the employees by department, and then running the `Sum` reduction on them.

```csharp
var departmentSales = _peopleAggregations.Apply(x=>x.RecordShell.Sales * x.RecordShell.SalesAdjustment, "AdjustedSales")
   .GroupBy(x=>x.RecordShell.DepartmentNumber)
   .Sum(x=>x["AdjustedSales"]);
```

#### Running Multiple Reductions Without Grouping

If you don't want to run grouping and just wanted to run a few reductions on your data, perhaps you want the Mean, Median, and Standard Deviation for employee sales. Then, you can execute a `GroupBy` with an open anon type and chain the reductions together.

```csharp
var reductions = _peopleAggregations
   .GroupBy(x=>new {})
   .Average(x=>x.RecordShell.Sales)
   .Quantile(x=>x.RecordShell.Sales,.5)
   .StandardDeviation(x=>x.RecordShell.Sales);
```

#### Accessing the Results of Reductions

The client stores the output of each reduction function in the `Aggregations` property of the `AggregationResult`. The pattern used for determining the key name that is `{PropertyName}_{CommandPostFix}` - usually the command postfix is simply a SCREAMING_CAPS representation of the method name or abbreviation. For an exact mapping, see below.

e.g. `Age_AVG`

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

### Apply Expressions

Apply expressions are operations that you can apply to each property in your pipeline. Apply supports basic Arithmetic operations (+, -, /, %, ^), as well as a plethora of string, math, DateTime, presence, and GeoDistance operations. To use an Apply Operation, you need to provide the expression you'd like to execute on the server, as well as the alias you'd like to store the result in the `Aggregations` dictionary. This specification allows the results from your apply functions to be used further down the pipeline.

If you had employees and you wanted to adjust the sales they had made by some factor. And your model had two fields, `Sales` & `SalesAdjustment`; you can create an `AdjustedSales` item for each employee which combines the two, e.g., by multiplying them together:

```csharp
var adjustments = _peopleAggregations.Apply(x=>x.RecordShell.Sales * x.RecordShell.SalesAdjustment, "AdjustedSales");
```

That `AdjustedSales` aggregation is then available for you to access directly from the AggregationResult object:

```csharp
foreach(var adjustment in _peopleAggregations)
{
   Console.WriteLine(adjustment["AdjustedSales"]);
}
```

Or, if you'd like to use that `AdjustedSales` further down the pipeline, you can do that as well. For example, if you wanted to find the average of the AdjustedSales for all your employees, you could:

```csharp
var departmentBySales = collection
   .Apply(x=>x.RecordShell.Sales * x.RecordShell.SalesAdjustment, "AdjustedSales")   
   .Average(x=>x["AdjustedSales"]);   
```

#### Supported Apply Functions:

The methods supported for Apply functions are standard string, math, timestamp operations you'd see in most programming languages. To use them inside an `Apply` predicate, invoke it as per the examples below. For Math and String methods, you invoke them as you would typically would them elsewhere in your code. For the timestamp, presence, and geo operations, there is a static class `ApplyFunctions` which you can invoke, which the expression parser will understand.

|Function|Type|Description|Example|
|--------|----|-----------|-------|
|log|Math|yields the 10 base log for the number|`Math.Log10(x["AdjustedSales"])`|
|abs|Math|yields the absolute value of the provided number|`Math.Abs(x["AdjustedSales"])`|
|ceil|Math|yields the smallest integer not less than the provided number|`Math.Ceil(x["AdjustedSales"])`|
|floor|Math|yields the smallest integer not greater than the provided number|`Math.Floof(x["AdjustedSales"])`|
|log2|Math|yields the Log base 2 for the provided number|`Math.Log(x["AdjustedSales"])`|
|exp|Math|yields the natural exponent for the provided number (e^y)|`Math.Exp(x["AdjustedSales"])`|
|sqrt|Math|yields the Square root for the provided number|`Math.Sqrt(x["AdjustedSales"])`|
|upper|String|yields the provided string to upper case|`x.RecordShell.Name.ToUpper()`|
|lower|String|yields the provided string to lower case|`x.RecordShell.Name.ToLower()`|
|startswith|String|Boolean expression - yields 1 if the string starts with the argument|`x.RecordShell.Name.StartsWith("bob")`|
|contains|String|Boolean expression - yields 1 if the string contains the argument |`x.RecordShell.Name.Contains("bob")`|
|substr|String|yields the substring starting at the given 0 based index, the length of the second argument, if the second argument is not provided, it will simply return the balance of the string|`x.RecordShell.Name.SubString(4, 10)`|
|format|string|Formats the string based off the provided pattern|`string.Format("Hello {0} You are {1} years old", x.RecordShell.Name, x.RecordShell.Age)`|
|split|string|Split's the string with the provided string - unfortunately if you are only passing in a single splitter, because of how expressions work, you'll need to provide string split options so that no optional parameters exist when building the expression, just pass `StringSplitOptions.None`|`x.RecordShell.Name.Split(",", StringSplitOptions.None)`|
|timefmt|time|transforms a unix timestamp to a formatted time string based off [strftime](http://strftime.org/) conventions|`ApplyFunctions.FormatTimestamp(x.RecordShell.LastTimeOnline)`|
|parsetime|time|Parsers the provided formatted timestamp to a unix timestamp|`ApplyFunctions.ParseTime(x.RecordShell.TimeString, "%FT%ZT")`|
|day|time|Rounds a unix timestamp to the beginning of the day|`ApplyFunctions.Day(x.RecordShell.LastTimeOnline)`|
|hour|time|Rounds a unix timestamp to the beginning of current hour|`ApplyFunctions.Hour(x.RecordShell.LastTimeOnline)`|
|minute|time|Round a unix timestamp to the beginning of the current minute|`ApplyFunctions.Minute(x.RecordShell.LastTimeOnline)`|
|month|time|Rounds a unix timestamp to the beginning of the current month|`ApplyFunctions.Month(x.RecordShell.LastTimeOnline)`|
|dayofweek|time|Converts the unix timestamp to the day number with Sunday being 0|`ApplyFunctions.DayOfWeek(x.RecordShell.LastTimeOnline)`|
|dayofmonth|time|Converts the unix timestamp to the current day of the month (1..31)|`ApplyFunctions.DayOfMonth(x.RecordShell.LastTimeOnline)`|
|dayofyear|time|Converts the unix timestamp to the current day of the year (1..31)|`ApplyFunctions.DayOfYear(x.RecordShell.LastTimeOnline)`|
|year|time|Converts the unix timestamp to the current year|`ApplyFunctions.Year(x.RecordShell.LastTimeOnline)`|
|monthofyear|time|Converts the unix timestamp to the current month (0..11)|`ApplyFunctions.MonthOfYear(x.RecordShell.LastTimeOnline)`|
|geodistance|geo|yields the distance between two points|`ApplyFunctions.GeoDistance(x.RecordShell.Home, x.RecordShell.Work)`|


## FAQ

* **Does Apply support String interpolation?** Not yet; rather than using string interpolation, you'll need to use `string.Format`
* **Do bitwise operations work for apply?** No - the Bitwise XOR operator `^` indicates an exponential relationship between the operands
* **When the Aggregation materializes, there's nothing in the `RecordShell` object. What gives?** The `RecordShell` item is used to preserve the original index through the aggregation pipeline and should only be used for operations within the pipeline. It will never materialize when the pipeline is enumerated
* **Why Do some Reductive aggregations condense down to a single number while others condense down to an IEnumerable?** When you build your pipeline, if you have a reductive aggregation not associated with a group, the aggregation is run immediately. The result of that reduction is furnished to you immediately for use.