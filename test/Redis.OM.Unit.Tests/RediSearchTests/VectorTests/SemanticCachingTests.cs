using System;
using System.Linq;
using Redis.OM.Contracts;
using Redis.OM.Vectorizers;
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

    [SkipIfMissingEnvVar("REDIS_OM_OAI_TOKEN")]
    public void OpenAISemanticCache()
    {
        var token = Environment.GetEnvironmentVariable("REDIS_OM_OAI_TOKEN");
        Assert.NotNull(token);
        var cache = _provider.OpenAISemanticCache(token, threshold: .15);
        cache.Store("What is the capital of France?", "Paris");
        var res = cache.GetSimilar("What really is the capital of France?").First();
        Assert.Equal("Paris",res.Response);
        Assert.True(res.Score < .15);
    }

    [SkipIfMissingEnvVar("REDIS_OM_HF_TOKEN")]
    public void HuggingFaceSemanticCache()
    {
        var token = Environment.GetEnvironmentVariable("REDIS_OM_HF_TOKEN");
        Assert.NotNull(token);
        var cache = _provider.HuggingFaceSemanticCache(token, threshold: .15);
        cache.Store("What is the capital of France?", "Paris");
        var res = cache.GetSimilar("What really is the capital of France?").First();
        Assert.Equal("Paris",res.Response);
        Assert.True(res.Score < .15);
    }

    [SkipIfMissingEnvVar("REDIS_OM_AZURE_OAI_TOKEN")]
    public void AzureOpenAISemanticCache()
    {
        var token = Environment.GetEnvironmentVariable("REDIS_OM_AZURE_OAI_TOKEN");
        var resource = Environment.GetEnvironmentVariable("REDIS_OM_AZURE_OAI_RESOURCE");
        var deployment = Environment.GetEnvironmentVariable("REDIS_OM_AZURE_OAI_DEPLOYMENT");
        var dimStr = Environment.GetEnvironmentVariable("REDIS_OM_AZURE_OAI_DIM");
        if (string.IsNullOrEmpty(dimStr) || !int.TryParse(dimStr, out var dim))
        {
            throw new InvalidOperationException("REDIS_OM_AZURE_OAI_DIM must contain a valid integrer value.");
        }
            
        
        Assert.NotNull(token);
        Assert.NotNull(resource);
        Assert.NotNull(deployment);
        var cache = _provider.AzureOpenAISemanticCache(token, resource, deployment, dim);
        cache.Store("What is the capital of France?", "Paris");
        var res = cache.GetSimilar("What really is the capital of France?").First();
        Assert.Equal("Paris",res.Response);
        Assert.True(res.Score < .15);

    }
}