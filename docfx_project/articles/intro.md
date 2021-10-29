# Getting Started

Redis OM is designed to make using Redis easier for .NET developers, so naturally the first question one might ask is how would you use it to connect to Redis?

The Redis OM library is an abstraction above a lower level (closer to Redis) library—[StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)—which it uses to manage connections to Redis. That is however, an implementation detail which should not be a concern to the user. `RedisConnectionProvider` class contains the connection logic, and provides for connections to Redis. The RedisConnectionProvider should only be initialized once in your app's lifetime.

## Initializing RedisConnectionProvider

RedisConnectionProvider takes a [Redis URI](https://github.com/redis-developer/Redis-Developer-URI-Spec/blob/main/spec.md) and uses that to initialize a connection to Redis.

Consequentially, all that needs to be done to initialize the client is calling the constructor of `RedisConnectionProvider` with a Redis uri. Alternatively, you can connect with a ConnectionConfiguration object.

### Connecting to a Standalone Instance of Redis No Auth

```csharp
var provider = new RedisConnectionProvider("redis://hostname:port");
```

### Connecting to Standalone Instance of Redis Just Password

```csharp
var provider = new RedisConnectionProvider("redis://:password@hostname:port");
```

### Connecting to Standalone Instance of Redis or Redis Enterprise Username and Password

```csharp
var provider = new RedisConnectionProvider("redis://username:password@hostname:port");
```

### Connecting to Standalone Instance of Redis Particular Database

```csharp
var provider = new RedisConnectionProvider("redis://username:password@hostname:port/4");
```

### Connecting to Redis Sentinel

When connecting to Redis Sentinel, you will need to provide the sentinel 

```csharp
var provider = new RedisConnectionProvider("redis://username:password@sentinel-hostname:port?endpoint=another-sentinel-host:port&endpoint=yet-another-sentinel-hot:port&sentinel_primary_name=redisprimary");
```

### Connecting to Redis Cluster

Connecting to a Redis Cluster is similar to connecting to a standalone server, it is advisable however to include at least one other alternative endpoint in the URI as a query parameter in case of a failover event.

```csharp
var provider = new RedisConnectionProvider("redis://username:password@hostname:port?endpoint=another-primary-host:port");
```

## Getting Connection RedisCollection RedisAggregationSet

There are three primary drivers of Redis in this Library, which can all be accessed from the `provider` object after it's been initialize.

* The RedisConnection - this provides a command level interface to Redis, a limited set of commands are directly implemented, but any command can be executed via the `Execute` and `ExecuteAsync` commands. To get a handle to the RedisConnection just use `provider.Connection`
* `RedisCollection<T>` - This is a generic collection used to access Redis. It provides a fluent interface for retrieving data stored in Redis. To create a `RedisCollection<T>` use `provider.RedisCollection<T>()`
* `RedisAggregationSet<T>` - This is another generic collection used to aggregate data in Redis. It provides a fluent interface for performing mapping & reduction operations on Redis. To create a `RedisAggregationSet<T>`use `provider.AggregationSet<T>()`

