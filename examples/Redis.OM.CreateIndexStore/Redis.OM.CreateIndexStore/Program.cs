using Redis.OM;
using Redis.OM.CreateIndexStore;
using Redis.OM.CreateIndexStore.Models;

#region Setup provides,indexes and seed

var provider = new RedisConnectionProvider("redis://172.29.252.187:6379");

provider.Connection.CreateIndex(typeof(Customer));
provider.Connection.CreateIndex(typeof(Employee));
provider.Connection.CreateIndex(typeof(Store));

var customers = provider.RedisCollection<Customer>();
var employee = provider.RedisCollection<Employee>();
var stores = provider.RedisCollection<Store>();

await SeedDataHelpers.SeedEmployess(employee);
await SeedDataHelpers.SeedCustomers(customers);
await SeedDataHelpers.SeedStore(stores);

#endregion Setup provides,indexes and seed

#region Basic queries

IList<Store> allStores = await stores.ToListAsync();
IList<Employee> allEmployees = await employee.ToListAsync();
IList<Customer> allCustomers = await customers.ToListAsync();

Store? store = await stores.Where(x => x.Id == 600).FirstOrDefaultAsync();
Customer? newton = await customers.Where(x => x.FullName == "Albert Einstein").FirstOrDefaultAsync();

IList<Employee> fulltimeEmployees = await employee.Where(x => x.EmploymentType == EmploymentType.FullTime).ToListAsync();

#endregion Basic queries

#region Drop the indexes

await provider.Connection.DropIndexAsync(typeof(Employee));
await provider.Connection.DropIndexAsync(typeof(Customer));
await provider.Connection.DropIndexAsync(typeof(Store));

#endregion Drop the indexes