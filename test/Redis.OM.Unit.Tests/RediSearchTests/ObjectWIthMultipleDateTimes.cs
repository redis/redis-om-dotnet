using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json, Prefixes = new []{"obj"})]
public class ObjectWIthMultipleDateTimes
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }
    public DateTime DateTime1 { get; set; }
    public DateTime DateTime2 { get; set; }
}

[Document(Prefixes = new []{"obj"})]
public class ObjectWIthMultipleDateTimesHash
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }
    public DateTime DateTime1 { get; set; }
    public DateTime DateTime2 { get; set; }
}