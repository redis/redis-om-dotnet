using System;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class ObjectWithDateTimeOffset
    {
        public DateTimeOffset Offset { get; set; }
    }
}