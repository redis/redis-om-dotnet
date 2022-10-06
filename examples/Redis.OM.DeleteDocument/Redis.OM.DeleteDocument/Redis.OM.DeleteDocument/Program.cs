namespace Redis.OM.DeleteDocument
{
    public class Program
    {

        private static void Main(string[] args)
        {
            // connect
            var provider = new RedisConnectionProvider("redis://localhost:6379");
            var connection = provider.Connection;
            var customers = provider.RedisCollection<Customer>();
            var people = provider.RedisCollection<Person>();

            // Create index
            connection.CreateIndex(typeof(Customer));
            connection.CreateIndex(typeof(Person));

            // Insert Object
            customers.Insert(new Customer
            {
                FirstName = "James",
                LastName = "Bond",
                Email = "bondjamesbond@email.com",
                Age = 69
            });

            customers.Insert(new Customer
            {
                FirstName = "Peter",
                LastName = "Pan",
                Email = "peterpan@email.com",
                Age = 69
            });

            var personKey = people.Insert(new Person
            {
                FirstName = "Son",
                LastName = "Goku",
                Age = 43
            });

            people.Insert(new Person
            {
                FirstName = "Prince",
                LastName = "Vegeta",
                Age = 48
            });

            // You can delete document via delete or deleteAsync() method
            Console.WriteLine($"Customer Documents Before deleting {string.Join(", ", customers.Select(x => x.FirstName))}");
            customers.Delete(customers.Where(x => x.FirstName == "James").FirstOrDefault());
            Console.WriteLine($"Customer Documents after deleting {string.Join(", ", customers.Select(x => x.FirstName))}");

            // You can also delete document via unlink(key)
            Console.WriteLine($"Person Documents Before unlinking {string.Join(", ", people.Select(x => x.LastName))}");
            provider.Connection.Unlink($"{personKey}");
            Console.WriteLine($"Person Documents After unlinking {string.Join(", ", people.Select(x => x.LastName))}");
        }
    }

}