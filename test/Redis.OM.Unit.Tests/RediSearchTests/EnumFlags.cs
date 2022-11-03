using System;
using System.Text.Json.Serialization;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumFlags
{
    None = 0,
    One = 1 << 0,
    Two = 1 << 1,
    Three = 1 << 2
}