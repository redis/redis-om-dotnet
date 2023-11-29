using Redis.OM.Contracts;
using Redis.OM.Unit.Tests;

namespace Redis.OM.Vectorizer.Tests;

public class SentenceVectorizerTests
{
    private readonly IRedisConnectionProvider _provider;
    public SentenceVectorizerTests()
    {
        _provider = new RedisConnectionProvider("redis://localhost:6379");
    }

    [Fact]
    public void Test()
    {
        var connection = _provider.Connection;
        connection.Set(new DocWithVector
        {
            Sentence = Vector.Of("Hello world this is Hal."),
            ImagePath = Vector.Of("hal.jpg")
        });
    }
}