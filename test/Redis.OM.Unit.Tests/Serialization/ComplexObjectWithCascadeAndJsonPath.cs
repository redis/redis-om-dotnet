using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class ComplexObjectWithCascadeAndJsonPath
{
    [Indexed(CascadeDepth = 2)] public InnerObject InnerCascade { get; set; }
    [Indexed(JsonPath = "$.InnerInnerCascade", CascadeDepth = 2)] public InnerObject InnerJson { get; set; }
}

public class InnerObject
{
    [Indexed(JsonPath = "$.Tag")] public InnerInnerObject InnerInnerJson { get; set; }
    [Indexed(CascadeDepth = 1)] public InnerInnerObject InnerInnerCascade { get; set; }
    [Indexed(JsonPath = "$.Tag")] public InnerInnerObject[] InnerInnerCollection { get; set; }
}

public class InnerInnerObject
{
    [Indexed] public string Tag { get; set; }
    [Indexed] public int Num { get; set; }
    [Indexed] public string[] Arr { get; set; }
}