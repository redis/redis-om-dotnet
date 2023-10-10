using System.Text;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
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
    public void Dave()
    {
        _connection.CreateIndex(typeof(ToyVector));

        var doubles = VectorUtils.VecStrToDoubles("I'm_sorry_Dave,_I'm_afraid_I_can't_do_that......");
        var obj = new ToyVector()
        {
            Id = "foo",
            SimpleVector = doubles
        };
        _connection.Set(obj);
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
        
        _connection.Set(obj);
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