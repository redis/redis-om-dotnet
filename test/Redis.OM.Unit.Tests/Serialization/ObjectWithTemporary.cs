using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(TemporaryExpirationSeconds = "1")]
public class ObjectWithTemporary
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed]
    public string Name { get; set; }
}