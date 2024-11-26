using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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

    [Searchable]
    public string? String2 { get; set; }
    
    [Indexed]
    public Guid? Guid { get; set; }
    
    [Indexed]
    public bool? Bool { get; set; }
    
    [Indexed]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AnEnum? Enum { get; set; }
}

[Document]
public class ObjectWithNullableStringsHash
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }

    [Indexed]
    public string? String1 { get; set; }

    [Searchable]
    public string? String2 { get; set; }
    
    [Indexed]
    public Guid? Guid { get; set; }
    
    [Indexed]
    public bool? Bool { get; set; }
}