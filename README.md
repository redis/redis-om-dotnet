<div align="center">
  <br/>
  <br/>
  <img width="360" src="images/logo.svg" alt="Redis OM" />
  <br/>
  <br/>
</div>

<p align="center">
    <p align="center">
        Object mapping, and more, for Redis and .NET
    </p>
</p>

---

[![NuGet](http://img.shields.io/nuget/v/Redis.OM.svg?style=flat-square)](https://www.nuget.org/packages/Redis.OM/)
[![License][license-image]][license-url]
[![Build Status][ci-svg]][ci-url]



**Redis OM .NET** makes it easy to model Redis data in your .NET Applications.

**Redis OM .NET** | [Redis OM Node.js](https://github.com/redis/redis-om-node) | [Redis OM Spring](https://github.com/redis/redis-om-spring) | [Redis OM Python](https://github.com/redis/redis-om-python)

<details>
  <summary><strong>Table of contents</strong></summary>

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

- [💡 Why Redis OM?](#-why-redis-om)
- [💻 Installation](#-installation)
- [🏁 Getting started](#-getting-started)
  - [Starting Redis](#starting-redis)
  - [📇 Modeling your domain (and indexing it!)](#-modeling-your-domain-and-indexing-it)
  - [🔑 Keys and Ids](#-keys-and-ids)
  - [🔎 Querying](#-querying)
  - [🖩 Aggregations](#-aggregations)
- [📚 Documentation](#-documentation)
- [⛏️ Troubleshooting](#-troubleshooting)
- [✨ Redis Stack](#-redis-stack)
  - [Why this is important](#why-this-is-important)
  - [So how do you get Redis Stack?](#so-how-do-you-get-redis-stack)
- [❤️ Contributing](#-contributing)
- [Connecting to Azure Managed Redis with EntraId](#connecting-to-azure-managed-redis-with-entraid)
  - [Prerequisites](#prerequisites)
  - [Connecting with EntraId](#connecting-with-entraid)
- [❤️ Our Contributors](#-our-contributors)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

</details>

## 💡 Why Redis OM?

Redis OM provides high-level abstractions for using Redis in .NET, making it easy to model and query your Redis domain objects.

This **preview** release contains the following features:

* Declarative object mapping for Redis objects
* Declarative secondary-index generation
* Fluent APIs for querying Redis
* Fluent APIs for performing Redis aggregations

## 💻 Installation

Using the dotnet cli, run:

```text
dotnet add package Redis.OM
```

## 🏁 Getting started

### Starting Redis

Before writing any code you'll need a Redis instance with the appropriate Redis modules! The quickest way to get this is with Docker:

```sh
docker run -p 6379:6379 -p 8001:8001 redis/redis-stack
```

This launches the [redis-stack](https://redis.io/docs/stack/) an extension of Redis that adds all manner of modern data structures to Redis. You'll also notice that if you open up http://localhost:8001 you'll have access to the redis-insight GUI, a GUI you can use to visualize and work with your data in Redis.

### 📇 Modeling your domain (and indexing it!)

With Redis OM, you can model your data and declare indexes with minimal code. For example, here's how we might model a customer object:

```csharp
[Document(StorageType = StorageType.Json)]
public class Customer
{
   [Indexed] public string FirstName { get; set; }
   [Indexed] public string LastName { get; set; }
   public string Email { get; set; }
   [Indexed(Sortable = true)] public int Age { get; set; }
   [Indexed] public string[] NickNames {get; set;}
}
```

Notice that we've applied the `Document` attribute to this class. We've also specified that certain fields should be `Indexed`.

Now we need to create the Redis index. So we'll connect to Redis and then call `CreateIndex` on an `IRedisConnection`:


```csharp
var provider = new RedisConnectionProvider("redis://localhost:6379");
provider.Connection.CreateIndex(typeof(Customer));
```

Redis OM provides limited support for schema migration at this time. You can check if the index definition in Redis matches your current index definition using the `IsIndexCurrent` method on the `RedisConnection`. Then you may use that output to determine when to re-create your indexes when your types change. An example implementation of this would look like:

```csharp
var provider = new RedisConnectionProvider("redis://localhost:6379");
var definition = provider.Connection.GetIndexInfo(typeof(Customer));

if (!provider.Connection.IsIndexCurrent(typeof(Customer)))
{
    provider.Connection.DropIndex(typeof(Customer));
    provider.Connection.CreateIndex(typeof(Customer));
}
```


### Indexing Embedded Documents

There are two methods for indexing embedded documents with Redis.OM, an embedded document is a complex object, e.g. if our `Customer` model had an `Address` property with the following model:

```csharp
[Document(IndexName = "address-idx", StorageType = StorageType.Json)]
public partial class Address
{
    public string StreetName { get; set; }
    public string ZipCode { get; set; }
    [Indexed] public string City { get; set; }
    [Indexed] public string State { get; set; }
    [Indexed(CascadeDepth = 1)] public Address ForwardingAddress { get; set; }
    [Indexed] public GeoLoc Location { get; set; }
    [Indexed] public int HouseNumber { get; set; }
}
```

#### Index By JSON Path

You can index fields by JSON path, in the top level model, in this case `Customer` you can decorate the `Address` property with an `Indexed` and/or `Searchable` attribute, specifying the JSON path to the desired field:

```csharp
[Document(StorageType = StorageType.Json)]
public class Customer
{
   [Indexed] public string FirstName { get; set; }
   [Indexed] public string LastName { get; set; }
   public string Email { get; set; }
   [Indexed(Sortable = true)] public int Age { get; set; }
   [Indexed] public string[] NickNames {get; set;}
   [Indexed(JsonPath = "$.ZipCode")]
   [Searchable(JsonPath = "$.StreetAddress")]
   public Address Address {get; set;}
}
```

##### Indexing Arrays of Objects

This methodology can also be used for indexing string and string-like value-types within objects within Arrays and Lists, so for example if we had an array of Addresses, and we wanted to index the cities within those addresses we could do so with the following

```cs
[Indexed(JsonPath = "$.City")]
public Address[] Addresses { get; set; }
```

Those Cities can then be queried with an `Any` predicate within the main `Where` clause.

```cs
collection.Where(c=>c.Addresses.Any(a=>a.City == "Satellite Beach"))
```

###### Limitations

The way Redis indexes fields within a collection of embedded objects does not allow multiple predictates to be specified to a given document e.g.

```cs
collection.Where(c=>c.Addresses.Any(a=>a.City == "Satellite Beach" && a.ZipCode == "32937))
```

In the above case the query can only check if the Addresses collection contains an entry that is `Satellite Beach`, and Contains an entry that has a zip code of `32937`, rather than an entry that has both the city of `Satellite Beach` and a  zip code of `32937

#### Cascading Index

Alternatively, you can also embedded models by cascading indexes. In this instance you'd simply decorate the property with `Indexed` and set the `CascadeDepth` to whatever to however may levels you want the model to cascade for. The default is 0, so if `CascadeDepth` is not set, indexing an object will be a no-op:

```csharp
[Document(StorageType = StorageType.Json)]
public class Customer
{
   [Indexed] public string FirstName { get; set; }
   [Indexed] public string LastName { get; set; }
   public string Email { get; set; }
   [Indexed(Sortable = true)] public int Age { get; set; }
   [Indexed] public string[] NickNames {get; set;}
   [Indexed(CascadeDepth = 2)]
   public Address Address {get; set;}
}
```

In the above case, all indexed/searchable fields in Address will be indexed down 2 levels, so the `ForwardingAddress` field in `Address` will also be indexed.

Once the index is created, we can:

* Insert Customer objects into Redis
* Get a Customer object by ID from Redis
* Query Customers from Redis
* Run aggregations on Customers in Redis

Let's see how!

### Indexing DateTimes

As of version 0.4.0, all DateTime objects are indexed as numerics, and they are inserted as numerics into JSON documents. Because of this, you can query them as if they were numerics!

### 🔑 Keys and Ids

#### ULIDs and strings

Ids are unique per object, and are used as part of key generation for the primary index in Redis. The natively supported Id type in Redis OM is the [ULID][ulid-url]. You can bind ids to your model, by explicitly decorating your Id field with the `RedisIdField` attribute:

```csharp
[Document(StorageType = StorageType.Json)]
public class Customer
{
    [RedisIdField] public Ulid Id { get; set; }
    [Indexed] public string FirstName { get; set; }
    [Indexed] public string LastName { get; set; }
    public string Email { get; set; }
    [Indexed(Sortable = true)] public int Age { get; set; }
    [Indexed] public string[] NickNames { get; set; }
}
```

When you call `Set` on the `RedisConnection` or call `Insert` in the `RedisCollection`, to insert your object into Redis, Redis OM will automatically set the id  for you and you will be able to access it in the object. If the `Id` type is a string, and there is no explicitly overriding IdGenerationStrategy on the object, the ULID for the object will bind to the string.

#### Other types of ids

Redis OM also supports other types of ids, ids must either be strings or value types (e.g. ints, longs, GUIDs etc. . .), if you want a non-ULID id type, you must either set the id on each object prior to insertion, or you must register an `IIdGenerationStrategy` with the `DocumentAttribute` class.

##### Register IIdGenerationStrategy

To Register an `IIdGenerationStrategy` with the `DocumentAttribute` class, simply call `DocumentAttribute.RegisterIdGenerationStrategy` passing in the strategy name, and the implementation of `IIdGenerationStrategy` you want to use. Let's say for example you had the `StaticIncrementStrategy`, which maintains a static counter in memory, and increments ids based off that counter:

```csharp
public class StaticIncrementStrategy : IIdGenerationStrategy
{
    public static int Current = 0;
    public string GenerateId()
    {
        return (Current++).ToString();
    }
}
```

You would then register that strategy with Redis.OM like so:

```csharp
DocumentAttribute.RegisterIdGenerationStrategy(nameof(StaticIncrementStrategy), new StaticIncrementStrategy());
```

Then, when you want to use that strategy for generating the Ids of a document, you can simply set the IdGenerationStrategy of your document attribute to the name of the strategy.

```csharp
[Document(IdGenerationStrategyName = nameof(StaticIncrementStrategy))]
public class ObjectWithCustomIdGenerationStrategy
{
    [RedisIdField] public string Id { get; set; }
}
```

#### Key Names

The key names are, by default, the fully qualified class name of the object, followed by a colon, followed by the `Id`. For example, there is a Person class in the Unit Test project, an example id of that person class would be `Redis.OM.Unit.Tests.RediSearchTests.Person:01FTHAF0D1EKSN0XG67HYG36GZ`, because `Redis.OM.Unit.Tests.RediSearchTests.Person` is the fully qualified class name, and `01FTHAF0D1EKSN0XG67HYG36GZ` is the ULID (the default id type). If you want to change the prefix (the fully qualified class name), you can change that in the `DocumentAttribute` by setting the `Prefixes` property, which is an array of strings e.g.

```csharp
[Document(Prefixes = new []{"Person"})]
public class Person
```

> Note: At this time, Redis.OM will only use the first prefix in the prefix list as the prefix when creating a key name. However, when an index is created, it will be created on all prefixes enumerated in the Prefixes property

### 🔎 Querying

We can query our domain using expressions in LINQ, like so:

```csharp
var customers = provider.RedisCollection<Customer>();

// Insert customer
customers.Insert(new Customer()
{
    FirstName = "James",
    LastName = "Bond",
    Age = 68,
    Email = "bondjamesbond@email.com"
});

// Find all customers whose last name is "Bond"
customers.Where(x => x.LastName == "Bond");

// Find all customers whose last name is Bond OR whose age is greater than 65
customers.Where(x => x.LastName == "Bond" || x.Age > 65);

// Find all customers whose last name is Bond AND whose first name is James
customers.Where(x => x.LastName == "Bond" && x.FirstName == "James");

// Find all customers with the nickname of Jim
customers.Where(x=>x.NickNames.Contains("Jim"));
```

### Vectors

Redis OM .NET also supports storing and querying Vectors stored in Redis. 

A `Vector<T>` is a representation of an object that can be transformed into a vector by a Vectorizer.

A `VectorizerAttribute` is the abstract class you use to decorate your Vector fields, it is responsible for defining the logic to convert the object's that `Vector<T>` is a container for into actual vector embeddings needed. In the package `Redis.OM.Vectorizers` we provide vectorizers for HuggingFace, OpenAI, and AzureOpenAI to allow you to easily integrate them into your workflows.

#### Define a Vector in your Model.

To define a vector in your model, simply decorate a `Vector<T>` field with an `Indexed` attribute which defines the algorithm and algorithmic parameters and a `Vectorizer` attribute which defines the shape of the vectors, (in this case we'll use OpenAI):

```cs
[Document(StorageType = StorageType.Json)]
public class OpenAICompletionResponse
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed(DistanceMetric = DistanceMetric.COSINE, Algorithm = VectorAlgorithm.HNSW, M = 16)]
    [OpenAIVectorizer]
    public Vector<string> Prompt { get; set; }

    public string Response { get; set; }

    [Indexed]
    public string Language { get; set; }
    
    [Indexed]
    public DateTime TimeStamp { get; set; }
}
```

#### Insert Vectors into Redis

With the vector defined in our model, all we need to do is create Vectors of the generic type, and insert them with our model. Using our `RedisCollection`, you can do this by simply using `Insert`:

```cs
var collection = _provider.RedisCollection<OpenAICompletionResponse>();
var completionResult = new OpenAICompletionResponse
{
    Language = "en_us", 
    Prompt = Vector.Of("What is the Capital of France?"), 
    Response = "Paris", 
    TimeStamp = DateTime.Now - TimeSpan.FromHours(3)
};
collection.Insert(completionResult);
```

The Vectorizer will manage the embedding generation for you without you having to intervene.

#### Query Vectors in Redis

To query vector fields in Redis, all you need to do is use the `VectorRange` method on a vector within our normal LINQ queries, and/or use the `NearestNeighbors` with whatever other filters you want to use, here's some examples:

```cs
var prompt = "What really is the Capital of France?";

// simple vector range, find first within .15
var result = collection.First(x => x.Prompt.VectorRange(prompt, .15));

// simple nearest neighbors query, finds first nearest neighbor
result = collection.NearestNeighbors(x => x.Prompt, 1, prompt).First();

// hybrid query, pre-filters result set for english responses, then runs a nearest neighbors search.
result = collection.Where(x=>x.Language == "en_us").NearestNeighbors(x => x.Prompt, 1, prompt).First();

// hybrid query, pre-filters responses newer than 4 hours, and finds first result within .15
var ts = DateTimeOffset.Now - TimeSpan.FromHours(4);
result = collection.First(x=>x.TimeStamp > ts && x.Prompt.VectorRange(prompt, .15));
```

#### What Happens to the Embeddings?

With Redis OM, the embeddings can be completely transparent to you, they are generated and bound to the `Vector<T>` when you query/insert your vectors. If however you needed your embedding after the insertion/Query, they are available at `Vector<T>.Embedding`, and be queried either as the raw bytes, as an array of doubles or as an array of floats (depending on your vectorizer).

#### Configuration

The Vectorizers provided by the `Redis.OM.Vectorizers` package have some configuration parameters that it will pull in either from your `appsettings.json` file, or your environment variables (with your appsettings taking precedence).

| Configuration Parameter            | Description                                   |
|--------------------------------    |-----------------------------------------------|
| REDIS_OM_HF_TOKEN                  | HuggingFace Authorization token.              |
| REDIS_OM_OAI_TOKEN                 | OpenAI Authorization token                    |
| REDIS_OM_OAI_API_URL               | OpenAI URL                                    |
| REDIS_OM_AZURE_OAI_TOKEN           | Azure OpenAI api key                          |
| REDIS_OM_AZURE_OAI_RESOURCE_NAME   | Azure resource name                           |
| REDIS_OM_AZURE_OAI_DEPLOYMENT_NAME | Azure deployment                              |

### Semantic Caching

Redis OM also provides the ability to use Semantic Caching, as well as providers for OpenAI, HuggingFace, and Azure OpenAI to perform semantic caching. To use a Semantic Cache, simply pull one out of the RedisConnectionProvider and use `Store` to insert items, and `GetSimilar` to retrieve items. For example:

```cs
var cache = _provider.OpenAISemanticCache(token, threshold: .15);
cache.Store("What is the capital of France?", "Paris");
var res = cache.GetSimilar("What really is the capital of France?").First();
```

### ML.NET Based Vectorizers

We also provide the packages `Redis.OM.Vectorizers.ResNet18` and `Redis.OM.Vectorizers.AllMiniLML6V2` which have embedded models / ML Pipelines in them to
allow you to easily Vectorize Images and Sentences respectively without the need to depend on an external API.

### 🖩 Aggregations

We can also run aggregations on the customer object, again using expressions in LINQ:

```csharp
// Get our average customer age
customerAggregations.Average(x => x.RecordShell.Age);

// Format customer full names
customerAggregations.Apply(x => string.Format("{0} {1}", x.RecordShell.FirstName, x.RecordShell.LastName),
      "FullName");

// Get each customer's distance from the Mall of America
customerAggregations.Apply(x => ApplyFunctions.GeoDistance(x.RecordShell.Home, -93.241786, 44.853816),
      "DistanceToMall");
```

## 📚 Documentation

This README just scratches the surface. You can find a full tutorial on the [redis.io](https://redis.io/learn/develop/dotnet/redis-om-dotnet/add-and-retrieve-objects). All the summary docs for this library can be found on the repo's [github page](https://redis.github.io/redis-om-dotnet/).

## ⛏️ Troubleshooting

If you run into trouble or have any questions, we're here to help!

First, check the [FAQ](docs/faq.md). If you don't find the answer there,
hit us up on the [Redis Discord Server](http://discord.gg/redis).

## ✨ Redis Stack

Redis OM can be used with regular Redis for Object mapping and getting objects by their IDs. For more advanced features like indexing, querying, and aggregation, Redis OM is dependent on the [**Redis Stack**](https://redis.io/docs/stack/) platform, a collection of modules that extend Redis.

### Why this is important

Without Redis Stack, you can still use Redis OM to create declarative models backed by Redis.

We'll store your model data in Redis as Hashes, and you can retrieve models using their primary keys.

So, what won't work without Redis Stack?

1. You won't be able to nest models inside each other.
2. You won't be able to use our expressive queries to find object -- you'll only be able to query by primary key.

### So how do you get Redis Stack?

You can use Redis Stack with your self-hosted Redis deployment. Just follow the instructions for [Installing Redis Stack](https://redis.io/docs/stack/get-started/install/).

Don't want to run Redis yourself? Redis Stack is also available on Redis Cloud. [Get started here](https://redis.com/try-free/).

## ❤️ Contributing

We'd love your contributions! If you want to contribute please read our [Contributing](CONTRIBUTING.md) document.

## Connecting to Azure Managed Redis with EntraId

Redis OM .NET supports connecting to Azure Managed Redis instances using EntraId (formerly Azure AD) authentication. This allows you to securely connect to your Azure Redis instance without embedding connection secrets in your code.

### Prerequisites

1. Install the required NuGet packages:
   ```
   dotnet add package Microsoft.Azure.StackExchangeRedis
   dotnet add package Azure.Identity
   ```

2. Configure your Azure Redis instance to use EntraId authentication

### Connecting with EntraId

```csharp
using Azure.Identity;
using Redis.OM;
using StackExchange.Redis;

// Create configuration options for your Azure Redis endpoint
// Standard format for Azure Managed Redis: your-instance-name.region.redis.azure.net:10000
ConfigurationOptions options = new ConfigurationOptions
{
    EndPoints = { "your-instance-name.region.redis.azure.net:10000" }
};

// Configure for Azure with DefaultAzureCredential
await options.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());

// Connect to Redis using EntraId authentication
var muxer = ConnectionMultiplexer.Connect(options);

// Create Redis OM connection provider using the authenticated connection
var provider = new RedisConnectionProvider(muxer);

// Define a model for Redis OM
[Document(StorageType = StorageType.Json)]
public class Customer
{
    [RedisIdField] public string Id { get; set; }
    [Indexed] public string Name { get; set; }
    [Indexed] public string Email { get; set; }
    [Indexed(Sortable = true)] public int Age { get; set; }
}

// Create index if it doesn't exist
await provider.Connection.CreateIndexAsync(typeof(Customer));

// Get a Redis collection for your model
var customers = provider.RedisCollection<Customer>();

// Insert a new customer
await customers.InsertAsync(new Customer 
{
    Name = "Jane Smith",
    Email = "jane@example.com",
    Age = 32
});

// Query customers
var youngCustomers = await customers.Where(c => c.Age < 35).ToListAsync();
foreach (var customer in youngCustomers)
{
    Console.WriteLine($"Name: {customer.Name}, Email: {customer.Email}, Age: {customer.Age}");
}
```

The `DefaultAzureCredential` class will automatically try various authentication methods, including:
- Environment variables
- Managed Identity
- Visual Studio credentials
- Azure CLI credentials
- Interactive browser login

This approach is particularly useful for services deployed to Azure, as it allows you to use Managed Identity without hardcoding any secrets.

## ❤️ Our Contributors

* [@slorello89](https://github.com/slorello89)
* [@banker](https://github.com/banker)
* [@simonprickett](https://github.com/simonprickett)
* [@BenShapira](https://github.com/BenShapira)
* [@satish860](https://github.com/satish860)
* [@dracco1993](https://github.com/dracco1993)
* [@ecortese](https://github.com/ecortese)
* [@DanJRWalsh](https://github.com/DanJRWalsh)
* [@baldutech](https://github.com/baldutech)
* [@shacharPash](https://github.com/shacharPash)
* [@frostshoxx](https://github.com/frostshoxx)
* [@berviantoleo](https://github.com/berviantoleo)
* [@AmirEsdeki](https://github.com/AmirEsdeki)
* [@Zulander1](https://github.com/zulander1)
* [@Jeevananthan](https://github.com/Jeevananthan-23)
* [@mariusmuntean](https://github.com/mariusmuntean)
* [@jcreus1](https://github.com/jcreus1)
* [@JuliusMikkela](https://github.com/JuliusMikkela)
* [@imansafari1991](https://github.com/imansafari1991)
* [@AndersenGans](https://github.com/AndersenGans)
* [@mdrakib](https://github.com/mdrakib)
* [@jrpavoncello](https://github.com/jrpavoncello)
* [@axnetg](https://github.com/axnetg)
* [@abbottdev](https://github.com/abbottdev)
* [@PrudiusVladislav](https://github.com/PrudiusVladislav)
* [@CormacLennon](https://github.com/CormacLennon)
* [@ahmedisam99](https://github.com/ahmedisam99)
* [@kirollosonsi](https://github.com/kirollosonsi)
* [@tgmoore](https://github.com/tgmoore)

<!-- Logo -->
[Logo]: images/logo.svg

<!-- Badges -->

[ci-svg]: https://github.com/redis-developer/redis-developer-dotnet/actions/workflows/dotnet-core.yml/badge.svg
[ci-url]: https://github.com/redis-developer/redis-developer-dotnet/actions/workflows/dotnet-core.yml
[license-image]: https://img.shields.io/badge/License-MIT-red.svg
[license-url]: LICENSE

<!-- Links -->

[redis-developer-website]: https://developer.redis.com
[redis-om-js]: https://github.com/redis-developer/redis-om-node
[redis-om-python]: https://github.com/redis-developer/redis-om-python
[redis-om-spring]: https://github.com/redis-developer/redis-om-spring
[redisearch-url]: https://oss.redis.com/redisearch/
[redis-json-url]: https://oss.redis.com/redisjson/
[pydantic-url]: https://github.com/samuelcolvin/pydantic
[ulid-url]: https://github.com/ulid/spec
