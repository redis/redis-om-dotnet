using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class ObjectWithDateTimeOffset
    {
        public DateTimeOffset Offset { get; set; }
    }

    [Document(StorageType = StorageType.Json)]
    public class ObjectWithDateTimeOffsetJson
    {
        [Indexed]
        [RedisIdField]
        public string Id { get; set; }

        public DateTimeOffset Offset { get; set; }
        public DateTime DateTime { get; set; }
    }
}