using Redis.OM.Contracts;
using Redis.OM.Searching;
using Redis.OM.Vectorizers.AllMiniLML6V2;

namespace Redis.OM.Vectorizer.Tests;

public class VectorizerFunctionalTests
{
    private readonly IRedisConnectionProvider _provider;
    public VectorizerFunctionalTests()
    {
        var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost";
        _provider = new RedisConnectionProvider($"redis://{host}");
    }

    [Fact]
    public void Test()
    {
        var connection = _provider.Connection;
        connection.DropIndex(typeof(DocWithVectors));
        connection.CreateIndex(typeof(DocWithVectors));
        connection.Set(new DocWithVectors
        {
            Sentence = Vector.Of("Hello world this is Hal."),
            ImagePath = Vector.Of("hal.jpg")
        });

        var collection = new RedisCollection<DocWithVectors>(connection);

        // images
        var res = collection.NearestNeighbors(x => x.ImagePath!, 5, "hal.jpg");
        Assert.Equal(0, res.First().Scores!.NearestNeighborsScore);
        // sentences
        collection.NearestNeighbors(x => x.Sentence!, 5, "Hello world this really is Hal.");
    }

    [Fact]
    public void VectorRangeUnaryExpressionTest()
    {
        var connection = _provider.Connection;
        connection.DropIndex(typeof(DocWithVectors));
        connection.CreateIndex(typeof(DocWithVectors));
        connection.Set(new DocWithVectors
        {
            Sentence = Vector.Of("Hello world this is Hal."),
            ImagePath = Vector.Of("hal.jpg")
        });
        
        var variableRange = 5;
        var collection = new RedisCollection<DocWithVectors>(connection);

        // images
        var res = collection.Where(x => x.Sentence!.VectorRange("Hal", variableRange, "score"));

        Assert.NotNull(res.First().Scores!.RangeScore);
        Assert.InRange(res.First().Scores!.RangeScore!.Value, 0, 1);

        // sentences
        collection.NearestNeighbors(x => x.Sentence!, variableRange, "Hello world this really is Hal.");
    }

    [Fact]
    public void VectorRangeMemberExpressionTest()
    {
        var connection = _provider.Connection;
        connection.DropIndex(typeof(DocWithVectors));
        connection.CreateIndex(typeof(DocWithVectors));
        connection.Set(new DocWithVectors
        {
            Sentence = Vector.Of("Hello world this is Hal."),
            ImagePath = Vector.Of("hal.jpg")
        });
        
        double variableRange = 5 + 5;
        var collection = new RedisCollection<DocWithVectors>(connection);

        // images
        
        var res = collection.Where(x => x.Sentence!.VectorRange("Hal", variableRange, "score"));

        Assert.NotNull(res.First().Scores!.RangeScore);
        Assert.InRange(res.First().Scores!.RangeScore!.Value, 0, 1);

        // sentences
        collection.NearestNeighbors(x => x.Sentence!, 5, "Hello world this really is Hal.");
    }

    private static double GetVectorRange() => 5;
    private static int GetKnnNeighbours() => 5;

    [Fact]
    public void VectorRangeMethodExpressionTest()
    {
        var connection = _provider.Connection;
        connection.DropIndex(typeof(DocWithVectors));
        connection.CreateIndex(typeof(DocWithVectors));
        connection.Set(new DocWithVectors
        {
            Sentence = Vector.Of("Hello world this is Hal."),
            ImagePath = Vector.Of("hal.jpg")
        });
        
        var collection = new RedisCollection<DocWithVectors>(connection);

        // images
        
        var res = collection.Where(x => x.Sentence!.VectorRange("Hal", GetVectorRange(), "score"));

        Assert.NotNull(res.First().Scores!.RangeScore);
        Assert.InRange(res.First().Scores!.RangeScore!.Value, 0, 1);

        // sentences
        collection.NearestNeighbors(x => x.Sentence!, GetKnnNeighbours(), "Hello world this really is Hal.");
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