using Redis.OM;
using Redis.OM.CreateIndexStore;

#region Setup provides and indexes

var provider = new RedisConnectionProvider("redis://172.29.252.187:6379");

provider.Connection.CreateIndex(typeof(Customer));
provider.Connection.CreateIndex(typeof(Employee));
provider.Connection.CreateIndex(typeof(Store));

var customers = provider.RedisCollection<Customer>();
var employee = provider.RedisCollection<Employee>();
var stores = provider.RedisCollection<Store>();

#endregion Setup provides and indexes

#region Seed for stores

await stores.InsertAsync(new Store() { Name = "CF Toronto Eaton Centre", FullAddress = "220 Yonge St, Toronto, ON M5B 2H1", Id = 599 });
await stores.InsertAsync(new Store() { Name = "Yorkdale Shopping Centre", FullAddress = "3401 Dufferin St, Toronto, ON M6A 2T9", Id = 600 });

#endregion Seed for stores

#region Seed for customer

await customers.InsertAsync(new Customer()
{
    Id = Guid.NewGuid(),
    Email = "Albert.Einstein@hotmail.com",
    FullName = "Albert Einstein",
    Publications = new[] { "Conclusions Drawn from the Phenomena of Capillarity", "Foundations of the General Theory of Relativity", "Investigations of Brownian Motion" },
    Address = new Address("4891 Island Hwy", "Campbell River")
});
await customers.InsertAsync(new Customer()
{
    Id = Guid.NewGuid(),
    Email = "INewton@hotmail.com",
    FullName = "Isaac Newton",
    Publications = new[] { "Philosophiæ Naturalis Principia Mathematica", "Opticks", "De mundi systemate" },
    Address = new Address("2019 90th Avenue", "Delia")
});
await customers.InsertAsync(new Customer()
{
    Id = Guid.NewGuid(),
    Email = "Galileo.Galilei@gmail.com",
    FullName = "Galileo Galilei",
    Publications = new[] { "Sidereus Nuncius", "The Assayer" }
});
await customers.InsertAsync(new Customer()
{
    Id = Guid.NewGuid(),
    Email = "MarieCurie@princeton.edu",
    FullName = "Marie Curie",
    Publications = new[] { "Recherches sur les substances radioactives", "Traité de Radioactivité", "L'isotopie et les éléments isotopes" },
    Address = new Address("1704 rue Ontario Ouest", "Montréal")
});

#endregion Seed for customer

#region Seed for employees

await employee.InsertAsync(new Employee() { Id = "1", Age = 18, EmploymentType = EmploymentType.PartTime, FullName = "Jean Francois" });
await employee.InsertAsync(new Employee() { Id = "2", Age = 21, EmploymentType = EmploymentType.PartTime, FullName = "Bowman Sophie" });
await employee.InsertAsync(new Employee() { Id = "3", Age = 78, EmploymentType = EmploymentType.FullTime, FullName = "Will Quinn" });

#endregion Seed for employees

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