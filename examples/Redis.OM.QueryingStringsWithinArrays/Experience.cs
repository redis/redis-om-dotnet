using Redis.OM.Modeling;

namespace Redis.OM.QueryingStringsWithinArrays;

[Document(StorageType =StorageType.Json)]
public class Experience
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; } = default!;

    [Indexed]
    public string[] Skills { get; set; } = default!;

    public override string ToString()
    {
        return string.Join(" ", Skills);
    }
}
