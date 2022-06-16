![Redis OM logo][Logo]
<p align="center">
    <p align="center">
        Objecting mapping, and more, for Redis.
    </p>
</p>

---

[![License][license-image]][license-url]
[![Build Status][ci-svg]][ci-url]



**Redis OM .NET** makes it easy to model Redis data in your .NET Applications.

[**Redis OM .NET**][redis-om-dotnet] | [Redis OM Node.js][redis-om-js] | [Redis OM Spring][redis-om-spring] | [Redis OM Python][redis-om-python]

<details>
  <summary><strong>Table of contents</strong></summary>

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

- [💡 Why Redis OM?](#-why-redis-om)
- [💻 Installation](#-installation)
- [🏁 Getting started](#-getting-started)
  - [Starting Redis](#starting-redis)
  - [📇 Modeling your domain (and indexing it!)](#-modeling-your-domain-and-indexing-it)
  - [🔎 Querying](#-querying)
  - [🖩 Aggregations](#-aggregations)
- [📚 Documentation](#-documentation)
- [⛏️ Troubleshooting](#-troubleshooting)
- [✨ RediSearch and RedisJSON](#-redisearch-and-redisjson)
  - [Why this is important](#why-this-is-important)
  - [So how do you get RediSearch and RedisJSON?](#so-how-do-you-get-redisearch-and-redisjson)
- [❤️ Contributing](#-contributing)

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
docker run -p 6379:6379 redislabs/redismod:preview
```

### 📇 Modeling your domain (and indexing it!)

With Redis OM, you can model your data and declare indexes with minimal code. For example, here's how we might model a customer object:

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

Notice that we've applied the `Document` attribute to this class. We've also specified that certain fields should be `Indexed`.

Now we need to create the Redis index. So we'll connect to Redis and then call `CreateIndex` on an `IRedisConnection`:


```csharp
var provider = new RedisConnectionProvider("redis://localhost:6379");
provider.Connection.CreateIndex(typeof(Customer));
```

Once the index is created, we can:

* Insert Customer objects into Redis
* Get a Customer object by ID from Redis
* Query Customers from Redis
* Run aggregations on Customers in Redis

Let's see how!

### 🔎 Querying

We can query our domain using expressions in LINQ, like so:

```csharp
var customers = provider.RedisCollection<Customer>();
// Find all customers whose last name is "Bond"
customers.Where(x => x.LastName == "Bond");

// Find all customers whose last name is Bond OR whose age is greater than 65
customers.Where(x => x.LastName == "Bond" || x.Age > 65);

// Find all customers whose last name is Bond AND whose first name is James
customers.Where(x => x.LastName == "Bond" && x.FirstName == "James");
```

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

## ⛏️ Troubleshooting

If you run into trouble or have any questions, we're here to help!

First, check the [FAQ](docs/faq.md). If you don't find the answer there,
hit us up on the [Redis Discord Server](http://discord.gg/redis).

## ✨ RediSearch and RedisJSON

Redis OM relies on core features from two source-available Redis modules: **RediSearch** and **RedisJSON**.

These modules are the "magic" behind the scenes:

* RediSearch adds querying, indexing, and full-text search to Redis
* RedisJSON adds the JSON data type to Redis

### Why this is important

Without RediSearch or RedisJSON, you can still use Redis OM to create declarative models backed by Redis.

We'll store your model data in Redis as Hashes, and you can retrieve models using their primary keys.

So, what won't work without these modules?

1. Without RedisJSON, you won't be able to nest models inside each other.
2. Without RediSearch, you won't be able to use our expressive queries to find object -- you'll only be able to query by primary key.

### So how do you get RediSearch and RedisJSON?

You can use RediSearch and RedisJSON with your self-hosted Redis deployment. Just follow the instructions on installing the binary versions of the modules in their Quick Start Guides:

- [RedisJSON Quick Start - Running Binaries](https://oss.redis.com/redisjson/#download-and-running-binaries)
- [RediSearch Quick Start - Running Binaries](https://oss.redis.com/redisearch/Quick_Start/#download_and_running_binaries)

**NOTE**: Both quick start guides also have instructions on how to run these modules in Redis with Docker.

Don't want to run Redis yourself? RediSearch and RedisJSON are also available on Redis Cloud. [Get started here](https://redis.com/try-free/).

## ❤️ Contributing

We'd love your contributions!

**Bug reports** are especially helpful at this stage of the project. [You can open a bug report on GitHub](https://github.com/redis-developer/redis-developer-dotnet/issues/new).

You can also **contribute documentation** -- or just let us know if something needs more detail. [Open an issue on GitHub](https://github.com/redis-developer/redis-developer-dotnet/issues/new) to get started.

<!-- Logo -->
[Logo]: images/red.svg

<!-- Badges -->

[ci-svg]: https://github.com/redis-developer/redis-developer-dotnet/actions/workflows/dotnet-core.yml/badge.svg
[ci-url]: https://github.com/redis-developer/redis-developer-dotnet/actions/workflows/dotnet-core.yml
[license-image]: https://img.shields.io/badge/License-BSD%203--Clause-blue.svg
[license-url]: https://github.com/redis-developer/redis-om-dotnet/blob/main/LICENSE

<!-- Links -->

[redis-developer-website]: https://developer.redis.com
[redis-om-dotnet]: https://github.com/redis-developer/redis-om-dotnet
[redis-om-js]: https://github.com/redis-developer/redis-om-js
[redis-om-python]: https://github.com/redis-developer/redis-om-python
[redis-om-dotnet]: https://github.com/redis-developer/redis-om-dotnet
[redis-om-spring]: https://github.com/redis-developer/redis-om-spring
[redisearch-url]: https://oss.redis.com/redisearch/
[redis-json-url]: https://oss.redis.com/redisjson/
[pydantic-url]: https://github.com/samuelcolvin/pydantic
[ulid-url]: https://github.com/ulid/spec
