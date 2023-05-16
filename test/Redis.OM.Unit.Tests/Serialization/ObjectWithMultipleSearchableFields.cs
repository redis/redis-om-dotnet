using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Hash)]
public class ObjectWithMultipleSearchableFields
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; }

    [Searchable]
    public string FirstName { get; set; }

    [Searchable]
    public string LastName { get; set; }
}