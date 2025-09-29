using System;
using System.Collections.Generic;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithNumericArrays
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed]
    public int[] IntArray { get; set; }

    [Indexed]
    public long[] LongArray { get; set; }

    [Indexed]
    public double[] DoubleArray { get; set; }

    [Indexed]
    public short[]  ShortArray { get; set; }
}

[Document(StorageType = StorageType.Json)]
public class NestedObjectWithNumericArrays
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed(CascadeDepth = 1)]
    public ObjectWithNumericArrays Inner { get; set; }
}

[Document(StorageType = StorageType.Json)]
public class ObjectWithNumericLists
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed]
    public List<int> IntList { get; set; }

    [Indexed]
    public List<long> LongList { get; set; }

    [Indexed]
    public List<double> DoubleList { get; set; }

    [Indexed]
    public List<short> ShortList { get; set; }
}

[Document(StorageType = StorageType.Json)]
public class NestedObjectWithNumericLists
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed(CascadeDepth = 1)]
    public ObjectWithNumericLists Inner { get; set; }
}