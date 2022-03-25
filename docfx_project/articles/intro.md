# Getting Started


Redis OM is designed to make using Redis easier for .NET developers, so naturally the first question one might ask is where to start?

## Prerequisites

* A .NET Standard 2.0 compatible version of .NET. This means that all .NET Framework versions 4.6.1+, .NET Core 2.0+ and .NET 5+ will work with Redis OM .NET.
* An IDE for writing .NET, Visual Studio, Rider, VS Code will all work.

## Installation

To install Redis OM .NET all you need to do is add the [`Redis.OM`](https://www.nuget.org/packages/Redis.OM/) NuGet package to your project. This can be done by running `dotnet add package Redis.OM`

## Connecting to Redis.

The next major step for getting started with Redis OM .NET is to connect to Redis.

The Redis OM library is an abstraction above a lower level (closer to Redis) library—[StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)—which it uses to manage connections to Redis. That is however, an implementation detail which should not be a concern to the user. `RedisConnectionProvider` class contains the connection logic, and provides for connections to Redis. The RedisConnectionProvider should only be initialized once in your app's lifetime.

## Initializing RedisConnectionProvider

RedisConnectionProvider takes a [Redis URI](https://github.com/redis-developer/Redis-Developer-URI-Spec/blob/main/spec.md) and uses that to initialize a connection to Redis.

Consequentially, all that needs to be done to initialize the client is calling the constructor of `RedisConnectionProvider` with a Redis uri. Alternatively, you can connect with a ConnectionConfiguration object, or if you have a ConnectionMultiplexer in your DI container already, you can construct it with your ConnectionMultiplexer.

#### Connecting to a Standalone Instance of Redis No Auth

```csharp
var provider = new RedisConnectionProvider("redis://hostname:port");
```

#### Connecting to Standalone Instance of Redis Just Password

```csharp
var provider = new RedisConnectionProvider("redis://:password@hostname:port");
```

#### Connecting to Standalone Instance of Redis or Redis Enterprise Username and Password

```csharp
var provider = new RedisConnectionProvider("redis://username:password@hostname:port");
```

#### Connecting to Standalone Instance of Redis Particular Database

```csharp
var provider = new RedisConnectionProvider("redis://username:password@hostname:port/4");
```

#### Connecting to Redis Sentinel

When connecting to Redis Sentinel, you will need to provide the sentinel 

```csharp
var provider = new RedisConnectionProvider("redis://username:password@sentinel-hostname:port?endpoint=another-sentinel-host:port&endpoint=yet-another-sentinel-hot:port&sentinel_primary_name=redisprimary");
```

#### Connecting to Redis Cluster

Connecting to a Redis Cluster is similar to connecting to a standalone server, it is advisable however to include at least one other alternative endpoint in the URI as a query parameter in case of a failover event.

```csharp
var provider = new RedisConnectionProvider("redis://username:password@hostname:port?endpoint=another-primary-host:port");
```

## Getting the RedisConnection, RedisCollection, and RedisAggregationSet

There are three primary drivers of Redis in this Library, which can all be accessed from the `provider` object after it's been initialize.

* The RedisConnection - this provides a command level interface to Redis, a limited set of commands are directly implemented, but any command can be executed via the `Execute` and `ExecuteAsync` commands. To get a handle to the RedisConnection just use `provider.Connection`
* `RedisCollection<T>` - This is a generic collection used to access Redis. It provides a fluent interface for retrieving data stored in Redis. To create a `RedisCollection<T>` use `provider.RedisCollection<T>()`
* `RedisAggregationSet<T>` - This is another generic collection used to aggregate data in Redis. It provides a fluent interface for performing mapping & reduction operations on Redis. To create a `RedisAggregationSet<T>`use `provider.AggregationSet<T>()`

