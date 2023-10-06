using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Redis.OM.Contracts;
using Xunit;

namespace Redis.OM.Unit.Tests;

public class VectorIndexCreationTests
{
    private readonly IRedisConnection _substitute = Substitute.For<IRedisConnection>();
    
    [Fact]
    public void CreateIndexWithVector()
    {
        _substitute.ClearSubstitute();
        _substitute.Execute(Arg.Any<string>(), Arg.Any<string[]>()).Returns(new RedisReply("OK"));

        _substitute.CreateIndex(typeof(ObjectWithVector));
        _substitute.CreateIndex(typeof(ObjectWithVectorHash));
        _substitute.Received().Execute(
            "FT.CREATE",
            $"{nameof(ObjectWithVector).ToLower()}-idx",
            "ON",
            "Json",
            "PREFIX",
            "1",
            $"Redis.OM.Unit.Tests.{nameof(ObjectWithVector)}:",
            "SCHEMA",
            "$.SimpleHnswVector", "AS", "SimpleHnswVector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT64", "DIM", "10", "DISTANCE_METRIC", "L2",
            "$.SimpleVectorizedVector.Vector", "AS","SimpleVectorizedVector", "VECTOR", "FLAT", "6", "TYPE", "FLOAT32", "DIM", "30", "DISTANCE_METRIC", "L2"
        );
        
        _substitute.Received().Execute(
            "FT.CREATE",
            $"{nameof(ObjectWithVectorHash).ToLower()}-idx",
            "ON",
            "Hash",
            "PREFIX",
            "1",
            $"Redis.OM.Unit.Tests.{nameof(ObjectWithVectorHash)}:",
            "SCHEMA",
            "SimpleHnswVector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT64", "DIM", "10", "DISTANCE_METRIC", "L2",
            "SimpleVectorizedVector.Vector", "VECTOR", "FLAT", "6", "TYPE", "FLOAT32", "DIM", "30", "DISTANCE_METRIC", "L2"
        );
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
        
        var byteStringSimpleHnsw = Encoding.UTF8.GetString(simpleHnswHash.SelectMany(BitConverter.GetBytes).ToArray());
        var byteStringVectorizedFlashHash = Encoding.UTF8.GetString(vectorizedFlatHashVector.SelectMany(BitConverter.GetBytes).ToArray());

        var hashObj = new ObjectWithVectorHash()
        {
            Id = "foo",
            SimpleHnswVector = simpleHnswHash,
            SimpleVectorizedVector = "foobar"
        };

        var jsonObj = new ObjectWithVector()
        {
            Id = "foo",
            SimpleHnswVector = simpleHnswHash,
            SimpleVectorizedVector = "foobar"
        };

        var json =
            $"{{\"Id\":\"foo\",\"SimpleHnswVector\":{simpleHnswJsonStr},\"SimpleVectorizedVector\":{{\"Value\":\"\\u0022foobar\\u0022\",\"Vector\":{vectorizedFlatVectorJsonStr}}}}}";
        
        _substitute.Execute("HSET", Arg.Any<string[]>()).Returns(new RedisReply("3"));
        _substitute.Execute("JSON.SET", Arg.Any<string[]>()).Returns(new RedisReply("OK"));
        _substitute.Set(hashObj);
        _substitute.Set(jsonObj);
        _substitute.Received().Execute("HSET", "Redis.OM.Unit.Tests.ObjectWithVectorHash:foo", "Id", "foo", "SimpleHnswVector",
            byteStringSimpleHnsw, "SimpleVectorizedVector.Vector", byteStringVectorizedFlashHash, "SimpleVectorizedVector.Value", "foobar");
        _substitute.Received().Execute("JSON.SET", "Redis.OM.Unit.Tests.ObjectWithVector:foo", ".", json);
        var deseralized = JsonSerializer.Deserialize<ObjectWithVector>(json);
        Assert.Equal("foobar", deseralized.SimpleVectorizedVector);
    }
}