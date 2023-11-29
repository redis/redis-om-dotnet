using System;
using System.Threading.Tasks;
using Moq;
using Redis.OM.Modeling;
using StackExchange.Redis;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests;

public class RedisConnectionTests
{
    [Fact]
    public void RedisConnectionThrowsRedisOmException()
    {
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock.Setup(x => x.Execute("SomeCommand", It.IsAny<object[]>())).Throws(new RedisServerException("Something went wrong!"));
        var sut = new RedisConnection(databaseMock.Object);

        var exception = Assert.ThrowsAny<RedisOmException>(() => sut.Execute("SomeCommand", "Arg1", "Arg2"));

        Assert.NotNull(exception.InnerException);
        Assert.IsType<RedisServerException>(exception.InnerException);
    }

    [Fact]
    public void RedisConnectionThrowsRegularException()
    {
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock.Setup(x => x.Execute("SomeCommand", It.IsAny<object[]>())).Throws(new NotImplementedException("Something went wrong!"));
        var sut = new RedisConnection(databaseMock.Object);

        var exception = Assert.ThrowsAny<Exception>(() => sut.Execute("SomeCommand", "Arg1", "Arg2"));

        Assert.NotNull(exception.InnerException);
        Assert.IsType<NotImplementedException>(exception.InnerException);
    }

    [Fact]
    public void RedisConnectionThrowsRedisOmExceptionOnTimeoutException()
    {
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock.Setup(x => x.Execute("SomeCommand", It.IsAny<object[]>())).Throws(new RedisTimeoutException("Something went wrong!", CommandStatus.Unknown));
        var sut = new RedisConnection(databaseMock.Object);

        var exception = Assert.ThrowsAny<RedisOmException>(() => sut.Execute("SomeCommand", "Arg1", "Arg2"));

        Assert.NotNull(exception.InnerException);
        Assert.IsType<RedisTimeoutException>(exception.InnerException);
    }

    [Fact]
    public async Task RedisConnectionThrowsRedisOmExceptionAsync()
    {
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock.Setup(x => x.ExecuteAsync("SomeCommand", It.IsAny<object[]>())).ThrowsAsync(new RedisServerException("Something went wrong!"));
        var sut = new RedisConnection(databaseMock.Object);

        var exception = await Assert.ThrowsAnyAsync<RedisOmException>(() => sut.ExecuteAsync("SomeCommand", "Arg1", "Arg2"));

        Assert.NotNull(exception.InnerException);
        Assert.IsType<RedisServerException>(exception.InnerException);
    }

    [Fact]
    public async Task RedisConnectionThrowsRegularExceptionAsync()
    {
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock.Setup(x => x.ExecuteAsync("SomeCommand", It.IsAny<object[]>())).ThrowsAsync(new NotImplementedException("Something went wrong!"));
        var sut = new RedisConnection(databaseMock.Object);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => sut.ExecuteAsync("SomeCommand", "Arg1", "Arg2"));

        Assert.NotNull(exception.InnerException);
        Assert.IsType<NotImplementedException>(exception.InnerException);
    }

    [Fact]
    public async Task RedisConnectionThrowsRedisOmExceptionOnTimeoutExceptionAsync()
    {
        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock.Setup(x => x.ExecuteAsync("SomeCommand", It.IsAny<object[]>())).ThrowsAsync(new RedisTimeoutException("Something went wrong!", CommandStatus.Unknown));
        var sut = new RedisConnection(databaseMock.Object);

        var exception = await Assert.ThrowsAnyAsync<RedisOmException>(() => sut.ExecuteAsync("SomeCommand", "Arg1", "Arg2"));

        Assert.NotNull(exception.InnerException);
        Assert.IsType<RedisTimeoutException>(exception.InnerException);
    }
}