using System;
using Redis.OM.RediSearch;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Redis.OM;
using Redis.OM.Contracts;
using Xunit;

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
            var personIndexExists = false;
            var hashPersonIndexExists = false;

            try
            {
                Connection.Execute("FT.INFO", "person-idx");
                personIndexExists = true;
            }
            catch
            {
                // ignored
            }

            try
            {
                Connection.Execute("FT.INFO", "hash-person-idx");
                hashPersonIndexExists = true;
            }
            catch
            {
                // ignored
            }

            if(!personIndexExists)
                Connection.CreateIndex(typeof(RediSearchTests.Person));
            if (!hashPersonIndexExists)
                Connection.CreateIndex(typeof(RediSearchTests.HashPerson));
        }

        private IRedisConnection _connection = null;
        public IRedisConnection Connection
        {
            get
            {
                if (_connection == null)
                    _connection = GetConnection();
                return _connection;
            }
        }

        private IRedisConnection GetConnection()
        {
            var host = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost:6379";
            var connectionString = $"redis://{host}";
            var provider = new RedisConnectionProvider(connectionString);
            return provider.Connection;
        }        

        public void Dispose()
        {
            Connection.DropIndexAndAssociatedRecords(typeof(RediSearchTests.Person));
            Connection.DropIndexAndAssociatedRecords(typeof(RediSearchTests.HashPerson));
        }
    }
}
