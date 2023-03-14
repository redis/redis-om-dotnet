using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class BasicJsonObject
{
    [RedisIdField]
    public string Id { get; set; }
    public string Name { get; set; }
}

[Document(StorageType = StorageType.Json)]
public class BasicJsonObjectTestSave
{
    [RedisIdField]
    public string Id { get; set; }
    [Indexed]public string Name { get; set; }
}