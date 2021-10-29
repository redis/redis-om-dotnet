# Geo Filters

A really nifty bit of indexing you can do with Redis OM is geo-indexing. To GeoIndex, all you need to do is to mark a `GeoLoc` field in your model as `Indexed` and create the index 

```csharp
[Document]
public class Restaurant
{
    [Indexed]
    public string Name { get; set; }

    [Indexed]
    public GeoLoc Location{get; set;}

    [Indexed(Aggregatable = true)]
    public double CostPerPerson{get;set;}
}
```

So let's create the index and seed some data.

```csharp
// connect
var provider = new RedisConnectionProvider("redis://localhost:6379");

// get connection
var connection = provider.Connection;

// get collection
var restaurants = provider.RedisCollection<Restaurant>();

// Create index
await connection.CreateIndexAsync(typeof(Restaurant));

// seed with dummy data
 var r1 = new Restaurant {Name = "Tony's Pizza & Pasta", CostPerPerson = 12.00, Location = new (-122.076751,37.369929)};
var r2 = new Restaurant {Name = "Nizi Sushi", CostPerPerson = 16.00, Location = new (-122.057360,37.371207)};
var r3 = new Restaurant {Name = "Thai Thai", CostPerPerson = 11.50, Location = new (-122.04382,37.38)};
var r4 = new Restaurant {Name = "Chipotles", CostPerPerson = 8.50, Location = new (-122.0524,37.359719 )};
restaurants.Insert(r1);
restaurants.Insert(r2);
restaurants.Insert(r3);
restaurants.Insert(r4);
```

## Querying Based off Location

With our data seeded, we can now run geo-filters on our restaurants data, let's say we had an office (e.g. Redis's offices in Mountain View at `-122.064224,37.377266`) and we wanted to find nearby restaurants, we could do so by using a `GeoFilter` query restaurants within a certain radius, say 1 mile we can:

```csharp
var nearbyRestaurants = restaurants.GeoFilter(x => x.Location, -122.064224, 37.377266, 5, GeoLocDistanceUnit.Miles);
foreach (var restaurant in nearbyRestaurants)
{
    Console.WriteLine($"{restaurant.Name} is within 1 mile of work");
}
```
