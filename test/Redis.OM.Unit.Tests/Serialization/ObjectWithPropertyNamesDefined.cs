using System.Text.Json.Serialization;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithPropertyNamesDefined
{
    [JsonPropertyName("notKey")]
    [Indexed(PropertyName = "notKey")]
    [RedisIdField]
    public string Key { get; set; }
    
}