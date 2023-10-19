using System.Linq;
using System.Text;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests;

[Collection("Redis")]
public class VectorFunctionalTests
{
    private readonly IRedisConnection _connection;

    public VectorFunctionalTests(RedisSetup setup)
    {
        _connection = setup.Connection;
    }

    [Fact]
    public void BasicRangeQuery()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray(),
            SimpleVectorizedVector = "FooBarBaz"
        });
        var queryVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
        var res = collection.First(x => x.SimpleHnswVector.VectorRange(queryVector, 5));
        Assert.Equal("helloWorld", res.Id);
    }

    [Fact]
    public void MultiRangeOnSameProperty()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray(),
            SimpleVectorizedVector = "FooBarBaz"
        });
        var queryVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
        var res = collection.First(x => x.SimpleHnswVector.VectorRange(queryVector, 5) && x.SimpleHnswVector.VectorRange(queryVector, 6));
        Assert.Equal("helloWorld", res.Id);
    }

    [Fact]
    public void RangeAndKnn()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray(),
            SimpleVectorizedVector = "FooBarBaz"
        });
        var queryVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
        queryVector[0] += 2;
        var res = collection.NearestNeighbors(x=>x.SimpleVectorizedVector, 1, "FooBarBaz")
            .First(x => x.SimpleHnswVector.VectorRange(queryVector, 5, "range"));
        Assert.Equal("helloWorld", res.Id);
        Assert.Equal(4, res.VectorScores.RangeScore);
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }

    [Fact]
    public void BasicQuery()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray(),
            SimpleVectorizedVector = "FooBarBaz"
        });
        var queryVector = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
        queryVector[0] += 2;

        var res = collection.NearestNeighbors(x => x.SimpleVectorizedVector, 1, "FooBarBaz").First();
        Assert.Equal("helloWorld", res.Id);
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
        res = collection.NearestNeighbors(x => x.SimpleHnswVector, 1, queryVector).First();
        Assert.Equal(4, res.VectorScores.NearestNeighborsScore);
    }

    [Fact]
    public void ScoresOnHash()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(ObjectWithVectorHash));
        _connection.CreateIndex(typeof(ObjectWithVectorHash));
        var doubles = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var obj = new ObjectWithVectorHash
        {
            Id = "helloWorld",
            SimpleHnswVector = doubles,
            SimpleVectorizedVector = "foo",
        };
        var collection = new RedisCollection<ObjectWithVectorHash>(_connection);
        collection.Insert(obj);
        var res = collection.NearestNeighbors(x => x.SimpleHnswVector, 5, doubles).First();
        
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }
    
    [Fact]
    public void TestIndex()
    {
        _connection.CreateIndex(typeof(ObjectWithVectorHash));

        var doubles = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var obj = new ObjectWithVectorHash
        {
            Id = "helloWorld",
            SimpleHnswVector = doubles,
            SimpleVectorizedVector = "foo",
        };
        
        var key = _connection.Set(obj);
        var res = _connection.Get<ObjectWithVectorHash>(key);
        Assert.Equal(doubles, res.SimpleHnswVector);

        key = _connection.Set(new ObjectWithVector()
        {
            Id = "helloWorld",
            SimpleHnswVector = doubles,
            SimpleVectorizedVector = "foobarbaz"
        });

        var jsonRes = _connection.Get<ObjectWithVector>(key);
        
        Assert.Equal(doubles, jsonRes.SimpleHnswVector);
        Assert.Equal("foobarbaz", jsonRes.SimpleVectorizedVector);
    }

    [Fact]
    public void Insert()
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

        var hashObj = new ObjectWithVectorHash()
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswHash,
            SimpleVectorizedVector = "foobar"
        };

        var key = _connection.Set(hashObj);
        var res = _connection.Get<ObjectWithVectorHash>(key);
        Assert.Equal("foobar", res.SimpleVectorizedVector);
    }
}