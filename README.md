<h1 align="center">Redis OM</h1>
<p align="center">
    <p align="center">
        Objecting mapping and more, for Redis.
    </p>
</p>

---


Welcome to Redis OM .NET, a library that helps you use Redis in .NET Applications.

**Redis OM .NET** | [Redis OM Node.js][redis-om-js] | [Redis OM Spring][redis-om-spring] | [Redis OM Python][redis-om-python]

<details>
  <summary><strong>Table of contents</strong></summary>

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [➡ Why Redis OM?](#--why-redis-om-)
- [💻 Installation](#---installation)
- [🏁 Getting started](#---getting-started)
  * [Starting Redis](#starting-redis)
  * [📇 Creating an Index](#---creating-an-index)
  * [🔎 Querying](#---querying)
  * [(◕(' 人 ') ◕) Aggregations](#-------------aggregations)
- [📚 Documentation](#---documentation)
- [⛏️ Troubleshooting](#---troubleshooting)
- [✨ RediSearch and RedisJSON](#--redisearch-and-redisjson)
  * [Why this is important](#why-this-is-important)
  * [So how do you get RediSearch and RedisJSON?](#so-how-do-you-get-redisearch-and-redisjson-)
- [❤️ Contributing](#---contributing)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

</details>

## ➡ Why Redis OM?

Redis OM is a high-level library for using Redis in .NET this **preview** release contains the following features:

* A Declarative Object Mapper for Redis Objects.
* A Declarative secondary-index generation tool for Redis.
* A fluent API for searching for objects in Redis
* A fluent API for performing Aggregations in Redis.

## 💻 Installation

Installation is simple with the dotnet cli just run:

```text
dotnet add package Redis.OM
```

## 🏁 Getting started

### Starting Redis

Before writing any code you should probably have an instance of Redis to connect to! Starting Redis is easy with Docker!

```sh
docker run -p 6379:6379 redislabs/redismod:preview
```

### 📇 Creating an Index

With Redis OM you can model your data and declare indices with minimal code:

```csharp
[Document(StorageType = StorageType.Json)]
public class Customer
{
   [Indexed] public string FirstName { get; set; }
   [Indexed] public string LastName { get; set; }
   [Indexed] public string Email { get; set; }
   [Indexed(Sortable = true)] public int Age { get; set; }
}
```

After an index is declared on a type, creating the index is as easy as connecting to redis, and calling `CreateIndex` on an `IRedisConnection`

```csharp
var provider = new RedisConnectionProvider("redis://localhost:6379");
connection.CreateIndex(typeof(Customer));
```

With this done you can now:

* Insert Customer objects into Redis
* Get a Customer object by ID frm Redis
* Query Customers from Redis
* Run aggregations on Customers in Redis

### 🔎 Querying 

After an index is declared and created, querying can be done using expressions in LINQ:

```csharp
var customers = provider.RedisCollection<Customer>();
// Find all customers who's last name is "Bond"
customers.Where(x => x.LastName == "Bond");

// Find all customers who's last name is Bond OR who's age is greater than 65
customers.Where(x => x.LastName == "Bond" || x.Age > 65);

// Find all customer's who's last name is Bond AND who's first name is James
customers.Where(x => x.LastName == "Bond" && x.FirstName == "James");
```

### (◕(' 人 ') ◕) Aggregations

With the customer index created you can easily run aggregations on the customer object using expressions in LINQ:

```csharp
// Get Average Age
customerAggregations.Average(x => x.RecordShell.Age);

// Format Customer Full Names
customerAggregations.Apply(x => string.Format("{0} {1}", x.RecordShell.FirstName, x.RecordShell.LastName),
      "FullName");

// Get Customer Distance from Mall of America.
customerAggregations.Apply(x => ApplyFunctions.GeoDistance(x.RecordShell.Home, -93.241786, 44.853816),
      "DistanceToMall");
```

## 📚 Documentation

Documentation is available [here](docs/README.md).

## ⛏️ Troubleshooting

If you run into trouble or have any questions, we're here to help! 

First, check the [FAQ](docs/faq.md). If you don't find the answer there,
hit us up on the [Redis Discord Server](http://discord.gg/redis).

## ✨ RediSearch and RedisJSON

Redis OM relies on core features from two source available Redis modules: **RediSearch** and **RedisJSON**.

These modules are the "magic" behind the scenes:

* RediSearch adds querying, indexing, and full-text search to Redis
* RedisJSON adds the JSON data type to Redis

### Why this is important

Without RediSearch or RedisJSON installed, you can still use Redis OM to create declarative models backed by Redis.

We'll store your model data in Redis as Hashes, and you can retrieve models using their primary keys.

So, what won't work without these modules?

1. Without RedisJSON, you won't be able to nest models inside each other, like we did with the example model of a `Customer` model.
2. Without RediSearch, you won't be able to use our expressive queries to find models -- just primary keys.

### So how do you get RediSearch and RedisJSON?

You can use RediSearch and RedisJSON with your self-hosted Redis deployment. Just follow the instructions on installing the binary versions of the modules in their Quick Start Guides:

- [RedisJSON Quick Start - Running Binaries](https://oss.redis.com/redisjson/#download-and-running-binaries)
- [RediSearch Quick Start - Running Binaries](https://oss.redis.com/redisearch/Quick_Start/#download_and_running_binaries)

**NOTE**: Both Quick Start Guides also have instructions on how to run these modules in Redis with Docker.

Don't want to run Redis yourself? RediSearch and RedisJSON are also available on Redis Cloud. [Get started here.](https://redis.com/try-free/)

## ❤️ Contributing

We'd love your contributions!

**Bug reports** are especially helpful at this stage of the project. [You can open a bug report on GitHub](https://github.com/redis-developer/redis-developer-dotnet/issues/new).

You can also **contribute documentation** -- or just let us know if something needs more detail. [Open an issue on GitHub](https://github.com/redis-developer/redis-developer-dotnet/issues/new) to get started.
