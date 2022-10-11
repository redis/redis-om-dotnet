using Redis.OM.Modeling;

namespace Redis.OM.CreateIndexStore.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = new[] { "CANADA.STORE.TORONTO" }, IndexName = "Store1-idx")]
    public class Employee
    {
        [RedisIdField][Indexed] public string Id { get; set; } = null!;

        [Indexed] public string FullName { get; set; } = null!;

        [Indexed] public int Age { get; set; }

        [Indexed] public EmploymentType EmploymentType { get; set; }
    }

    public enum EmploymentType
    {
        FullTime,
        PartTime
    }
}