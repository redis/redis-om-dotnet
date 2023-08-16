using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithDateTime
{
    [RedisField]
    public string Id { get; set; }
    [Indexed(Sortable = true)]
    public DateTime Timestamp { get; set; }
    [Indexed(Sortable = true)]
    public DateTimeOffset TimestampOffset { get; set; }
    [Indexed]
    public DateTime? NullableTimestamp { get; set; }
}

[Document(StorageType = StorageType.Hash)]
public class ObjectWithDateTimeHash
{
    [RedisField]
    public string Id { get; set; }
    
    [Indexed]
    public DateTime Timestamp { get; set; }
    [Indexed]
    public DateTime? NullableTimestamp { get; set; }
}