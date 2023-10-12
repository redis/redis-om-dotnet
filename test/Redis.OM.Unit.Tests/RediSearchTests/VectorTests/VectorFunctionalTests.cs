using System.Linq;
using System.Text;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests;

[Collection("Redis")]
public class VectorFunctionalTests
{
    private IRedisConnection _connection = null;

    public VectorFunctionalTests(RedisSetup setup)
    {
        _connection = setup.Connection;
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

        var res = collection.NearestNeighbors(x => x.SimpleVectorizedVector, 1, "FooBarBaz").First();
        Assert.Equal("helloWorld", res.Id);
    }

    [Fact]
    public void Overflow()
    {
        var doubles = new double[]
        {
            1.79769313486231570e+308, 1.79769313486231570e+308, 1.79769313486231570e+308, 1.79769313486231570e+308,
            1.79769313486231570e+308, 1.79769313486231570e+308
        };

        var lowerDoubles = new double[]
            { -1.79769E+308, -1.79769E+308, -1.79769E+308, -1.79769E+308, -1.79769E+308, -1.79769E+308 };
        _connection.CreateIndex(typeof(ToyVector));
        var obj = new ToyVector()
        {
            Id = "1",
            SimpleVector = doubles
        };
        
        var collection = new RedisCollection<ToyVector>(_connection);
        collection.NearestNeighbors(x => x.SimpleVector, 1, lowerDoubles).First();
    }
    
    [Fact]
    public void Dave()
    {
        _connection.CreateIndex(typeof(ToyVector));

        // var doubles = VectorUtils.VecStrToDoubles("This vector's json result gets blown out oddly..");
        var doubles = VectorUtils.VecStrToDoubles("I'm sorry Dave, I'm afraid I can't do that......");
        // var doubles = new double[] { 0, 1, 2, 3, 4, 5 };
        var obj = new ToyVector()
        {
            Id = "1",
            SimpleVector = doubles
        };
        _connection.Set(obj);

        var collection = new RedisCollection<ToyVector>(_connection);
        collection.NearestNeighbors(x => x.SimpleVector, 1, doubles).First();
    }

    

    [Fact]
    public void TestIndex()
    {
        _connection.CreateIndex(typeof(ObjectWithVectorHash));

        var doubles = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var obj = new ObjectWithVectorHash
        {
            Id = "foo",
            SimpleHnswVector = doubles,
            SimpleVectorizedVector = "foo",
        };
        
        var key = _connection.Set(obj);
        var res = _connection.Get<ObjectWithVectorHash>(key);
        Assert.Equal(doubles, res.SimpleHnswVector);

        key = _connection.Set(new ObjectWithVector()
        {
            Id = "foo",
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
            Id = "foo",
            SimpleHnswVector = simpleHnswHash,
            SimpleVectorizedVector = "foobar"
        };

        var key = _connection.Set(hashObj);
        var res = _connection.Get<ObjectWithVectorHash>(key);
        Assert.Equal("foobar", res.SimpleVectorizedVector);
    }
}