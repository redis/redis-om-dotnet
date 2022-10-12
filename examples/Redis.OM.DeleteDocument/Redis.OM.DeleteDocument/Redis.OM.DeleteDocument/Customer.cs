using Redis.OM.Modeling;

namespace Redis.OM.DeleteDocument
{
    [Document(StorageType = StorageType.Json)]
    internal class Customer
    {
        [RedisIdField][Indexed] public string? Id { get; set; }
        [Indexed] public string FirstName { get; set; }
        [Indexed] public string LastName { get; set; }
        [Indexed] public string Email { get; set; }
        [Indexed(Aggregatable = true)] public int Age { get; set; }
        [Indexed(Aggregatable = true)] public GeoLoc Home { get; set; }
    }
}
