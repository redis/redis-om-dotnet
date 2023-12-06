using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests;

public class VectorIndexCreationTests
{
    private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();
    
    [Fact]
    public void CreateIndexWithVector()
    {
        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply("OK"));

        _substitute.CreateIndex(typeof(ObjectWithVector));
        
        _substitute.Received().Execute(
            "FT.CREATE",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            "ON",
            "Json",
            "PREFIX",
            "1",
            $"Redis.OM.Unit.Tests.{nameof(ObjectWithVector)}:",
            "SCHEMA",
            "$.Name", "AS", "Name", "TAG", "SEPARATOR", "|", 
            "$.Num", "AS", "Num", "NUMERIC",
            "$.SimpleHnswVector", "AS", "SimpleHnswVector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT64", "DIM", "10", "DISTANCE_METRIC", "L2",
            "$.SimpleVectorizedVector.Vector", "AS","SimpleVectorizedVector", "VECTOR", "FLAT", "6", "TYPE", "FLOAT32", "DIM", "30", "DISTANCE_METRIC", "L2"
        );

        _substitute.ClearSubstitute();
        _substitute.CreateIndex(typeof(ObjectWithVectorHash));
        _substitute.Received().Execute(
            "FT.CREATE",
            $"{nameof(ObjectWithVectorHash).ToLower()}-idx",
            "ON",
            "Hash",
            "PREFIX",
            "1",
            $"Redis.OM.Unit.Tests.{nameof(ObjectWithVectorHash)}:",
            "SCHEMA",
            "Name", "TAG", "SEPARATOR", "|", 
            "Num", "NUMERIC",
            "SimpleHnswVector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT64", "DIM", "10", "DISTANCE_METRIC", "L2",
            "SimpleVectorizedVector.Vector", "AS", "SimpleVectorizedVector", "VECTOR", "FLAT", "6", "TYPE", "FLOAT32", "DIM", "30", "DISTANCE_METRIC", "L2"
        );
    }

    [Fact]
    public void SimpleKnnQuery()
    {
        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply(0));
        var collection = new RedisCollection<ObjectWithVector>(_substitute);
        var compVector = Vector.Of(new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        float[] floats = Enumerable.Range(0, 30).Select(x => (float)x).ToArray();
        var blob = compVector.Value.SelectMany(BitConverter.GetBytes).ToArray();
        var floatBlob = floats.SelectMany(BitConverter.GetBytes).ToArray();
        _ = collection.NearestNeighbors(x=>x.SimpleHnswVector, 5, compVector).ToList();

        _substitute.Received().Execute("FT.SEARCH",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            $"(*)=>[KNN 5 @SimpleHnswVector $0 AS {VectorScores.NearestNeighborScoreName}]",
            "PARAMS", 2, "0", Arg.Is<byte[]>(b=>b.SequenceEqual(blob)), "DIALECT", 2, "LIMIT", "0", "100");

        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply(0));
        var queryVector = Vector.Of("hello world");
        _ = collection.NearestNeighbors(x => x.SimpleVectorizedVector, 8, queryVector).ToArray();

        _substitute.Received().Execute("FT.SEARCH",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            $"(*)=>[KNN 8 @SimpleVectorizedVector $0 AS {VectorScores.NearestNeighborScoreName}]",
            "PARAMS", 2, "0", Arg.Is<byte[]>(b=>b.SequenceEqual(floatBlob)), "DIALECT", 2, "LIMIT", "0", "100");
    }

    [Fact]
    public void SimpleRangeQuery()
    {
        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply(0));
        var collection = new RedisCollection<ObjectWithVector>(_substitute);
        var compVector = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        float[] floats = Enumerable.Range(0, 30).Select(x => (float)x).ToArray();
        var floatBytes = floats.SelectMany(BitConverter.GetBytes).ToArray();
        var queryVector = Vector.Of("foobar");
        _ = collection.Where(x => x.SimpleVectorizedVector.VectorRange(queryVector, .3)).ToList();
        _substitute.Received().Execute("FT.SEARCH", $"{nameof(ObjectWithVector).ToLower()}-idx",
            "@SimpleVectorizedVector:[VECTOR_RANGE $0 $1]", "PARAMS", 4, "0", .3, "1", Arg.Is<byte[]>(b => b.SequenceEqual(floatBytes)),
            "DIALECT", 2, "LIMIT", "0", "100");

    } 
    
    [Fact]
    public void SimpleKnnQueryWithSortBy()
    {
        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply(0));
        var collection = new RedisCollection<ObjectWithVector>(_substitute);
        var compVector = Vector.Of(new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        float[] floats = Enumerable.Range(0, 30).Select(x => (float)x).ToArray();
        var blob = compVector.Value.SelectMany(BitConverter.GetBytes).ToArray();
        var floatBlob = floats.SelectMany(BitConverter.GetBytes).ToArray();
        _ = collection.NearestNeighbors(x=>x.SimpleHnswVector, 5, compVector).OrderBy(x=>x.VectorScores.NearestNeighborsScore).ToList();

        _substitute.Received().Execute("FT.SEARCH",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            $"(*)=>[KNN 5 @SimpleHnswVector $0 AS {VectorScores.NearestNeighborScoreName}]",
            "PARAMS", 2, "0", Arg.Is<byte[]>(b=>b.SequenceEqual(blob)), "DIALECT", 2, "LIMIT", "0", "100", "SORTBY", VectorScores.NearestNeighborScoreName, "ASC");

        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply(0));
        var queryVector = Vector.Of("hello world");
        _ = collection.NearestNeighbors(x => x.SimpleVectorizedVector, 8, queryVector).OrderByDescending(x=>x.VectorScores.NearestNeighborsScore).ToArray();

        _substitute.Received().Execute("FT.SEARCH",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            $"(*)=>[KNN 8 @SimpleVectorizedVector $0 AS {VectorScores.NearestNeighborScoreName}]",
            "PARAMS", 2, "0", Arg.Is<byte[]>(b=>b.SequenceEqual(floatBlob)), "DIALECT", 2, "LIMIT", "0", "100", "SORTBY", VectorScores.NearestNeighborScoreName, "DESC");
    }
        

    [Fact]
    public void TestBinConversions()
    {
        var piStr = VectorUtils.DoubleToVecStr(Math.PI);
        var pi = VectorUtils.DoubleFromVecStr(piStr);
        Assert.Equal(Math.PI, pi);
    }
    
    [Fact]
    public void InsertVectors()
    {
        var simpleHnswJsonStr = new StringBuilder();
        var vectorizedFlatVectorJsonStr = new StringBuilder();
        simpleHnswJsonStr.Append('[');
        vectorizedFlatVectorJsonStr.Append('[');
        var simpleHnswHash = new double[10];
        var vectorizedFlatHashVector = new float[30];
        for (var i = 0; i < 10; i++)
        {
            simpleHnswHash[i] = i;
        }
        for (var i = 0; i < 30; i++)
        {
            vectorizedFlatHashVector[i] = i;
        }

        simpleHnswJsonStr.Append(string.Join(',', simpleHnswHash));
        vectorizedFlatVectorJsonStr.Append(string.Join(',', vectorizedFlatHashVector));
        simpleHnswJsonStr.Append(']');
        vectorizedFlatVectorJsonStr.Append(']');

        var simpleHnswBytes = simpleHnswHash.SelectMany(BitConverter.GetBytes).ToArray();
        var flatVectorizedBytes = vectorizedFlatHashVector.SelectMany(BitConverter.GetBytes).ToArray();

        var simpleHnswVector = Vector.Of(simpleHnswHash);
        var simpleVectorizedVector = Vector.Of("foobar");

        var hashObj = new ObjectWithVectorHash()
        {
            Id = "foo",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        };

        var jsonObj = new ObjectWithVector()
        {
            Id = "foo",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        };

        var json =
            $"{{\"Id\":\"foo\",\"Num\":0,\"SimpleHnswVector\":{simpleHnswJsonStr},\"SimpleVectorizedVector\":{{\"Value\":\"\\u0022foobar\\u0022\",\"Vector\":{vectorizedFlatVectorJsonStr}}}}}";
        
        _substitute.Execute("HSET", Arg.Any<object[]>()).Returns(new RedisReply("3"));
        _substitute.Execute("JSON.SET", Arg.Any<object[]>()).Returns(new RedisReply("OK"));
        _substitute.Set(hashObj);
        _substitute.Set(jsonObj);
        _substitute.Received().Execute("HSET", "Redis.OM.Unit.Tests.ObjectWithVectorHash:foo", "Id", "foo", "Num", "0", "SimpleHnswVector",
            Arg.Is<byte[]>(x=>x.SequenceEqual(simpleHnswBytes)), "SimpleVectorizedVector.Vector", Arg.Is<byte[]>(x=>x.SequenceEqual(flatVectorizedBytes)), "SimpleVectorizedVector.Value", "\"foobar\"");
        _substitute.Received().Execute("JSON.SET", "Redis.OM.Unit.Tests.ObjectWithVector:foo", ".", json);
        var deseralized = JsonSerializer.Deserialize<ObjectWithVector>(json);
        Assert.Equal("foobar", deseralized.SimpleVectorizedVector.Value);
    }

    [Fact]
    public void HybridQuery()
    {
        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<object[]>()).Returns(new RedisReply(0));
        var collection = new RedisCollection<ObjectWithVector>(_substitute);
        var compVector = Vector.Of(new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        var blob = compVector.Value.SelectMany(BitConverter.GetBytes);
        _ = collection.Where(x => x.Name == "Steve" && x.Num < 5)
            .NearestNeighbors(x => x.SimpleHnswVector, 2, compVector).ToList();
        _substitute.Received().Execute("FT.SEARCH",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            $"(((@Name:{{Steve}}) (@Num:[-inf (5])))=>[KNN 2 @SimpleHnswVector $0 AS {VectorScores.NearestNeighborScoreName}]",
            "PARAMS", 2, "0", Arg.Is<byte[]>(b=>b.SequenceEqual(blob)), "DIALECT", 2, "LIMIT", "0", "100");
        
    }
}