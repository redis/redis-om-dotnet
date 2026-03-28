using Redis.OM.Modeling;
using System;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithNullableNumerics
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }

    [Indexed]
    public int? Int { get; set; }

    [Indexed]
    public long? Long { get; set; }

    [Indexed]
    public DateTime? DateTime { get; set; }
}

[Document]
public class ObjectWithNullableNumericsHash
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }

    [Indexed]
    public int? Int { get; set; }

    [Indexed]
    public long? Long { get; set; }

    [Indexed]
    public DateTime? DateTime { get; set; }
}
