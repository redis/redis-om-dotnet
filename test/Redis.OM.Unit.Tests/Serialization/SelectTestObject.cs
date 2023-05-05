using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class SelectTestObject
{
    [Indexed]
    public string Name { get; set; }
    public InnerObject Field1 { get; set; }
    public InnerObject Field2 { get; set; }
}

public class CongruentObject
{
    public InnerObject Field3 { get; set; }
    public InnerObject Field4 { get; set; }
}

public class CongruentObjectWithLikeNames
{
    public InnerObject Field1 { get; set; }
    public InnerObject Field2 { get; set; }
}