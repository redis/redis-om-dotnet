using System.Collections.Generic;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document]
public class HashTypeWithPrimitiveArray
{
    public string Name { get; set; }
    public bool[] Bools { get; set; }
    public int[] Ints { get; set; }
    public byte[] Bytes { get; set; }
    public sbyte[] SBytes { get; set; }
    public short[] Shorts { get; set; }
    public ushort[] UShorts { get; set; }
    public uint[] UInts { get; set; }
    public long[] Longs { get; set; }
    public ulong[] ULongs { get; set; }
    public char[] Chars { get; set; }
    public double[] Doubles { get; set; }
    public float[] Floats { get; set; }
    
    public List<int> IntList { get; set; }
}