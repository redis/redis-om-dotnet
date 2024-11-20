using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithNullableStrings
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }

    [Indexed]
    public string? String1 { get; set; }

    [Indexed]
    public string? String2 { get; set; }
}