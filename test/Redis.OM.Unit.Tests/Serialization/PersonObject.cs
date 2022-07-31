using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(StorageType = StorageType.Json)]
    public class Person
    {
        [RedisIdField] public Ulid Id { get; set; }
        [Indexed] public string Name { get; set; }
    }
}