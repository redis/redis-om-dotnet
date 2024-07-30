using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithMultipleSearchableAttributes
{
    [RedisIdField] public string Id { get; set; }
    
    [Searchable(JsonPath = "$.City")]
    [Searchable(JsonPath = "$.State")]
    public Address Address { get; set; }
}