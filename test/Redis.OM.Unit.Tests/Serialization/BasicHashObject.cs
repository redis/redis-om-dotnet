using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Hash)]
public class BasicHashObject
{
    [RedisIdField]
    public string Id { get; set; }
    public string Name { get; set; }
}