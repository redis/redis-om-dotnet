using Redis.OM;
using Redis.OM.UpdatingDocuments.Models;

const string CONNECTION_URI= "redis://localhost:6379";

// create test data
var coffee  = new Product() {
    Name = "coffee",
    Description = "a hot drink made from the roasted and ground coffee beans",
    Price = 2.00,
    DateAdded = DateTime.UtcNow,
    InStock = false
};

// connect and create index
var provider = new RedisConnectionProvider(CONNECTION_URI);
var connection = provider.Connection;
var products = provider.RedisCollection<Product>();
connection.CreateIndex(typeof(Product));

// insert data and save Id for querying later
var productId = await products.InsertAsync(coffee);

// update using IRedisCollection.Update
coffee.Description = "Pure developer fuel";
products.Update(coffee);

// update using IRedisCollection.UpdateAsync
coffee.Price = 2.50;
await products.UpdateAsync(coffee);

// update using IRedisCollection.Save
coffee.InStock = true;
products.Save(coffee);

// update using IRedisCollection.SaveAsync
coffee.DateAdded = DateTime.UtcNow.AddDays(-7);
await products.SaveAsync(coffee);

// query and validate results
var updatedCoffee = await products.FindByIdAsync(productId);

Console.WriteLine($"{nameof(Product.Name)} - Expected: {coffee.Name} Actual: {updatedCoffee?.Name}");
Console.WriteLine($"{nameof(Product.Description)} - Expected: {coffee.Description} Actual: {updatedCoffee?.Description}");
Console.WriteLine($"{nameof(Product.Price)} - Expected: {coffee.Price} Actual: {updatedCoffee?.Price}");
Console.WriteLine($"{nameof(Product.DateAdded)} - Expected: {coffee.DateAdded} Actual: {updatedCoffee?.DateAdded}");
Console.WriteLine($"{nameof(Product.InStock)} - Expected: {coffee.InStock} Actual: {updatedCoffee?.InStock}");