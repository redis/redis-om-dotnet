using Redis.OM;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using System;

namespace Redis.OM.RawQueryExample
{
    [Document(StorageType = StorageType.Json, IndexName = "person-idx")]
    public class Person
    {
        [RedisIdField]
        [Indexed]
        public string Id { get; set; } = Ulid.NewUlid().ToString();

        [Indexed]
        public string FirstName { get; set; }

        [Indexed]
        public string LastName { get; set; }

        [Searchable]
        public string FullText { get; set; }
        
        [Indexed(Sortable = true)]
        public int Age { get; set; }

        [Indexed]
        public string[] Skills { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var provider = new RedisConnectionProvider("redis://localhost:6379");
            var connection = provider.Connection;

            // Create index if it doesn't exist
            connection.CreateIndex(typeof(Person));

            // Add some sample data
            var collection = provider.RedisCollection<Person>();
            
            if (!collection.Any())
            {
                collection.Insert(new Person { FirstName = "John", LastName = "Doe", Age = 30, Skills = new[] { "C#", "Redis" }, FullText = "Hey Now Brown Cow"});
                collection.Insert(new Person { FirstName = "Jane", LastName = "Smith", Age = 28, Skills = new[] { "Java", "MongoDB", "Redis" } });
                collection.Insert(new Person { FirstName = "Bob", LastName = "Johnson", Age = 35, Skills = new[] { "Python", "Redis", "AWS" } });
                collection.Insert(new Person { FirstName = "Alice", LastName = "Williams", Age = 32, Skills = new[] { "JavaScript", "Node.js" } });
            }

            Console.WriteLine("=== Using standard LINQ expression ===");
            var linqResult = collection.Where(p => p.Age > 30).ToList();
            PrintResults(linqResult);

            Console.WriteLine("\n=== Using Raw query expression ===");
            var rawResult = collection.Raw("@Age:[31 +inf]").ToList();
            PrintResults(rawResult);

            Console.WriteLine("\n=== Using Raw query for tag search ===");
            var skillsResult = collection.Raw("@Skills:{Redis}").ToList();
            PrintResults(skillsResult);

            Console.WriteLine("\n=== Using Raw query with complex conditions ===");
            var complexResult = collection.Raw("@Age:[30 35] @Skills:{Redis}").ToList();
            PrintResults(complexResult);

            // Using Raw with AggregationSet properly
            Console.WriteLine("\n=== Using Raw with AggregationSet ===");
            
            // The proper way - Raw only sets the filter part, then use regular aggregation API
            var aggSet = provider.AggregationSet<Person>();
            var aggregationResult = aggSet.Raw("*")
                .GroupBy(x => x.RecordShell.Skills)
                .CountGroupMembers()
                .ToList();
            
            Console.WriteLine("Skills grouped by count:");
            foreach (var result in aggregationResult)
            {
                Console.WriteLine($"Skill: {result["Skills"]}, Count: {result["COUNT"]}");
            }
        }

        static void PrintResults(System.Collections.Generic.IEnumerable<Person> results)
        {
            foreach (var person in results)
            {
                Console.WriteLine($"{person.FirstName} {person.LastName}, Age: {person.Age}, Skills: {string.Join(", ", person.Skills)}");
            }
            Console.WriteLine($"Total results: {results.Count()}");
        }
    }
}