using Redis.OM.Modeling;

namespace Redis.OM.DeleteDocument
{
    [Document(StorageType = StorageType.Json, Prefixes = new[] { "Person" })]
    internal class Person
    {
        [RedisIdField][Indexed] public string? Id { get; set; }

        [Indexed] public string? FirstName { get; set; }

        [Indexed] public string? LastName { get; set; }

        [Indexed] public int Age { get; set; }
    }
}
