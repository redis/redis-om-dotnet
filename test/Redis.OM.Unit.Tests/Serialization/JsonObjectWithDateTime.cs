using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(StorageType = StorageType.Json)]
    public class JsonObjectWithDateTime
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public DateTime? NullableTime { get; set; }
    }
}