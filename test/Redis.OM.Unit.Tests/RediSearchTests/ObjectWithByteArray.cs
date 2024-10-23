using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json, Prefixes = new []{"obj"})]
public class ObjectWithByteArray
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }

    public byte[] Bytes1 { get; set; }

    public byte[] Bytes2 { get; set; }
}