using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class ObjectWithUlidId
    {
        [RedisIdField] public Ulid Id { get; set; }
    }
}