using Redis.OM.Contracts;
using Redis.OM.Unit.Tests;
using Redis.OM.Vectorizers.AllMiniLML6V2;

namespace Redis.OM.Vectorizer.Tests;

public class VectorizerFunctionalTests
{
    private readonly IRedisConnectionProvider _provider;
    public VectorizerFunctionalTests()
    {
        _provider = new RedisConnectionProvider("redis://localhost:6379");
    }

    [Fact]
    public void Test()
    {
        var connection = _provider.Connection;
        connection.Set(new DocWithVectors
        {
            Sentence = Vector.Of("Hello world this is Hal."),
            ImagePath = Vector.Of("hal.jpg")
        });
    }

    [Fact]
    public void SemanticCaching()
    {
        var cache = _provider.AllMiniLML6V2SemanticCache();
        cache.Store("What is the Capital of France?", "Paris");
        var res = cache.GetSimilar("What really is the capital of France?");
        Assert.NotEmpty(res);
        Assert.Equal("Paris", res.First().Response);
    }
}