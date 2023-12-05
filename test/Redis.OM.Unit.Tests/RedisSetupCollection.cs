using Redis.OM.Contracts;
using System;
using Redis.OM.Unit.Tests.RediSearchTests;
using Xunit;
using static Redis.OM.Unit.Tests.SearchJsonTests.RedisJsonNestedComposeValueTest;

namespace Redis.OM.Unit.Tests
{

    [CollectionDefinition("Redis")]
    public class RedisSetupCollection : ICollectionFixture<RedisSetup>
    {
    }
    public class RedisSetup : IDisposable
    {
        public RedisSetup()
        {
            Connection.CreateIndex(typeof(RediSearchTests.Person));
            Connection.CreateIndex(typeof(RediSearchTests.HashPerson));
            Connection.CreateIndex(typeof(ClassForEmptyRedisCollection));
            Connection.CreateIndex(typeof(ObjectWithStringLikeValueTypes));
            Connection.CreateIndex(typeof(ObjectWithStringLikeValueTypesHash));
            Connection.CreateIndex(typeof(ObjectWithEmbeddedArrayOfObjects));
            Connection.CreateIndex(typeof(ObjectWithZeroStopwords));
            Connection.CreateIndex(typeof(ObjectWithTwoStopwords));
            Connection.CreateIndex(typeof(ObjectWithDateTime));
            Connection.CreateIndex(typeof(ObjectWithDateTimeHash));
            Connection.CreateIndex(typeof(PersonWithNestedArrayOfObject));
            Connection.CreateIndex(typeof(ComplexObjectWithCascadeAndJsonPath));
            Connection.CreateIndex(typeof(BasicJsonObjectTestSave));
            Connection.CreateIndex(typeof(SelectTestObject));
            Connection.CreateIndex(typeof(ObjectWithDateTimeOffsetJson));
        }

        private IRedisConnectionProvider _provider;

        public IRedisConnectionProvider Provider => _provider ??= GetProvider();
        
        public IRedisConnection Connection => Provider.Connection;

        private IRedisConnectionProvider GetProvider()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost:6379";
            var connectionString = $"redis://{host}";
            return new RedisConnectionProvider(connectionString);
        }

        public void Dispose()
        {
            Connection.DropIndexAndAssociatedRecords(typeof(RediSearchTests.Person));
            Connection.DropIndexAndAssociatedRecords(typeof(RediSearchTests.HashPerson));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithStringLikeValueTypes));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithStringLikeValueTypesHash));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithEmbeddedArrayOfObjects));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithZeroStopwords));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithTwoStopwords));
            Connection.DropIndexAndAssociatedRecords(typeof(ClassForEmptyRedisCollection));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithDateTime));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithDateTimeHash));
            Connection.DropIndexAndAssociatedRecords(typeof(PersonWithNestedArrayOfObject));
            Connection.DropIndexAndAssociatedRecords(typeof(ComplexObjectWithCascadeAndJsonPath));
            Connection.DropIndexAndAssociatedRecords(typeof(BasicJsonObjectTestSave));
            Connection.DropIndexAndAssociatedRecords(typeof(SelectTestObject));
            Connection.DropIndexAndAssociatedRecords(typeof(ObjectWithDateTimeOffsetJson));
        }
    }
}
