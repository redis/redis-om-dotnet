using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json)]
public class SimpleObject
{
    [RedisIdField] public string Id { get; set; }
    [Indexed]
    public string Name { get; set; }
}

[Document(StorageType = StorageType.Hash)]
public class SimpleObjectHash
{
    [Indexed]
    public string Name { get; set; }
}