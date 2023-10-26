using System;
using System.Linq;
using Redis.OM.Contracts;
using Xunit;

namespace Redis.OM.Unit.Tests;

[Collection("Redis")]
public class SemanticCachingTests
{
    private readonly IRedisConnectionProvider _provider;
    public SemanticCachingTests(RedisSetup setup)
    {
        _provider = setup.Provider;
    }

    [Fact]
    public void OpenAISemanticCache()
    {
        var token = Environment.GetEnvironmentVariable("REDIS_OM_OAI_TOKEN");
        Assert.NotNull(token);
        var cache = _provider.OpenAISemanticCache(token);
        cache.Store("What is the capital of France?", "Paris");
        var res = cache.GetSimilar("What really is the capital of France?").First();
        Assert.Equal("Paris",res.Response);
        Assert.True(res.Score < .15);
    }

    [Fact]
    public void HuggingFaceSemanticCache()
    {
        var token = Environment.GetEnvironmentVariable("REDIS_OM_HF_TOKEN");
        Assert.NotNull(token);
        var cache = _provider.HuggingFaceSemanticCache(token);
        cache.Store("What is the capital of France?", "Paris");
        var res = cache.GetSimilar("What really is the capital of France?").First();
        Assert.Equal("Paris",res.Response);
        Assert.True(res.Score < .15);
    }
}