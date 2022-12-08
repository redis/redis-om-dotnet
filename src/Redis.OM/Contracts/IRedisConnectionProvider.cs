using Redis.OM.Aggregation;
using Redis.OM.Searching;

namespace Redis.OM.Contracts
{
    /// <summary>
    /// Provides a connection to redis.
    /// </summary>
    public interface IRedisConnectionProvider
    {
        /// <summary>
        /// Gets a command level interface to redis.
        /// </summary>
        IRedisConnection Connection { get; }

        /// <summary>
        /// Gets an aggregation set for redis.
        /// </summary>
        /// <typeparam name="T">The indexed type to run aggregations on.</typeparam>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>the aggregation set.</returns>
        RedisAggregationSet<T> AggregationSet<T>(int chunkSize = 100);

        /// <summary>
        /// Gets a redis collection.
        /// </summary>
        /// <typeparam name="T">The type the collection will be retrieving.</typeparam>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>A RedisCollection.</returns>
        IRedisCollection<T> RedisCollection<T>(int chunkSize = 100)
          where T : notnull;

        /// <summary>
        /// Gets a redis collection.
        /// </summary>
        /// <typeparam name="T">The type the collection will be retrieving.</typeparam>
        /// <param name="saveState">Whether or not the RedisCollection should maintain the state of documents it enumerates.</param>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>A RedisCollection.</returns>
        IRedisCollection<T> RedisCollection<T>(bool saveState, int chunkSize = 100)
          where T : notnull;
    }
}
