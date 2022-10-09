using Redis.OM.Modeling;

namespace Redis.OM.CreateIndexStore
{
    [Document(StorageType = StorageType.Json)]
    public class Customer
    {
        [RedisIdField][Indexed] public Guid Id { get; set; }

        [Indexed] public string FullName { get; set; } = null!;
        [Indexed] public string Email { get; set; } = null!;

        [Indexed] public string[] Publications { get; set; } = null!;

        [Indexed] public Address? Address { get; set; }
    }
}