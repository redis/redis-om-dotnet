using Redis.OM.Modeling;

namespace Redis.OM.CreateIndexStore
{
    [Document(StorageType = StorageType.Hash)]
    internal record Store
    {
        [RedisIdField][Indexed] public int Id { get; set; }

        [Indexed] public string FullAddress { get; set; } = null!;
        [Indexed] public string Name { get; set; } = null!;
    }

    [Document(StorageType = StorageType.Json, Prefixes = new[] { "CANADA.STORE.TORONTO" }, IndexName = "Store1-idx")]
    internal class Employee
    {
        [RedisIdField][Indexed] public string Id { get; set; } = null!;

        [Indexed] public string FullName { get; set; } = null!;

        [Indexed] public int Age { get; set; }

        [Indexed] public EmploymentType EmploymentType { get; set; }
    }

    internal enum EmploymentType
    {
        FullTime,
        PartTime
    }
}