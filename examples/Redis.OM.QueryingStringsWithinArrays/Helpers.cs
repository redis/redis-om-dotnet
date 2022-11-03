using Redis.OM.Searching;

namespace Redis.OM.QueryingStringsWithinArrays;

public class Helpers
{
    public static async Task AddDummyExperiences(IRedisCollection<Experience> collection)
    {
        List<Experience> experiences = new()
        {
            new Experience { Skills = new string[] { "C#", ".NET", "ASP.NET Core" } },
            new Experience { Skills = new string[] { "C#", "Android", "MAUI" } },
            new Experience { Skills = new string[] { "Java", "JVM", "Spring Boot" } },
            new Experience { Skills = new string[] { "Java", "Kotlin", "Android" } },
            new Experience { Skills = new string[] { "Dart", "Flutter", "Android" } }
        };
        foreach (var item in experiences)
        {
            await collection.InsertAsync(item);
        }
    }
}
