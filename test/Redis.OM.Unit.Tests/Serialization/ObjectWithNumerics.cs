using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithNumerics
{
    [Indexed]
    public int Integer { get; set; }
    [Indexed]
    public byte Byte { get; set; }
    [Indexed]
    public sbyte SByte { get; set; }
    [Indexed]
    public short Short { get; set; }
    [Indexed]
    public ushort UShort { get; set; }
    [Indexed]
    public uint UInt { get; set; }
    [Indexed]
    public long Long { get; set; }
    [Indexed]
    public ulong ULong { get; set; }
    [Indexed]
    public double Double { get; set; }
    [Indexed]
    public float Float { get; set; }
    [Indexed]
    public decimal Decimal { get; set; }
}