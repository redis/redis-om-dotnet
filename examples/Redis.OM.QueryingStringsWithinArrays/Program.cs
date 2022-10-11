using Redis.OM;
using Redis.OM.QueryingStringsWithinArrays;

var provider = new RedisConnectionProvider("redis://localhost:6379");
provider.Connection.CreateIndex(typeof(Experience));
var experiences = provider.RedisCollection<Experience>();
 
await Helpers.AddDummyExperiences(experiences);

//Contains usage 
var filteredExperiences = await experiences.Where(x=>x.Skills.Contains("C#")).ToListAsync();

foreach (var item in filteredExperiences)
{
    Console.WriteLine(item.ToString());
}