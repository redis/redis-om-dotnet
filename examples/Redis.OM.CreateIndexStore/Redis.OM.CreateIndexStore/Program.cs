using Redis.OM;
using Redis.OM.CreateIndexStore;
using Redis.OM.CreateIndexStore.Models;

var provider = new RedisConnectionProvider("redis://localhost:6379");

provider.Connection.CreateIndex(typeof(Customer));
provider.Connection.CreateIndex(typeof(Employee));
provider.Connection.CreateIndex(typeof(Store));

var customers = provider.RedisCollection<Customer>();
var employee = provider.RedisCollection<Employee>();
var stores = provider.RedisCollection<Store>();

await SeedDataHelpers.SeedEmployess(employee);
await SeedDataHelpers.SeedCustomers(customers);
await SeedDataHelpers.SeedStore(stores);

//retrieve all the contents
var allStores = await stores.ToListAsync();
var allEmployees = await employee.ToListAsync();
var allCustomers = await customers.ToListAsync();

//Filter by diffenrent properties
var store = await stores.Where(x => x.Id == 600).FirstOrDefaultAsync();
var newton = await customers.Where(x => x.FullName == "Albert Einstein").FirstOrDefaultAsync();
var fulltimeEmployees = await employee.Where(x => x.EmploymentType == EmploymentType.FullTime).ToListAsync();

//Drop the index
await provider.Connection.DropIndexAsync(typeof(Employee));
await provider.Connection.DropIndexAsync(typeof(Customer));
await provider.Connection.DropIndexAsync(typeof(Store));