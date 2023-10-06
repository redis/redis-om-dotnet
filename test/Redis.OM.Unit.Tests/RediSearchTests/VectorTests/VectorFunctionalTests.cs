using System.Text;
using Redis.OM.Contracts;
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