using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Redis.OM.Modeling;

namespace Redis.OM.Searching
{
    /// <summary>
    /// A collection of items in redis that you can use to look up items in redis, or perform queries on indexed documents.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    public interface IRedisCollection<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        /// <summary>
        /// Gets the collection state manager.
        /// </summary>
        RedisCollectionStateManager StateManager { get; }

        /// <summary>
        /// Gets the size of chunks to use when paginating.
        /// </summary>
        int ChunkSize { get; }

        /// <summary>
        /// Saves the current state of the collection, overriding what was initially materialized.
        /// </summary>
        void Save();

        /// <summary>
        /// Saves the current state of the collection, overriding what was initially materialized.
        /// </summary>
        /// <returns>a value task.</returns>
        ValueTask SaveAsync();

        /// <summary>
        /// Inserts an item into redis.
        /// </summary>
        /// <param name="item">an item.</param>
        /// <returns>the key.</returns>
        string Insert(T item);

        /// <summary>
        /// Inserts an item into redis.
        /// </summary>
        /// <param name="item">an item.</param>
        /// <returns>the key.</returns>
        Task<string> InsertAsync(T item);

        /// <summary>
        /// finds an item by it's ID.
        /// </summary>
        /// <param name="id">the id to lookup.</param>
        /// <returns>the item if it's present.</returns>
        Task<T?> FindByIdAsync(string id);

        /// <summary>
        /// finds an item by it's ID.
        /// </summary>
        /// <param name="id">the id to lookup.</param>
        /// <returns>the item if it's present.</returns>
        T? FindById(string id);

        /// <summary>
        /// Checks to see if anything matching the expression exists.
        /// </summary>
        /// <param name="expression">the expression to be matched.</param>
        /// <returns>Whether anything matching the expression was found.</returns>
        bool Any(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Updates the provided item in Redis. Document must have a property marked with the <see cref="RedisIdFieldAttribute"/>.
        /// </summary>
        /// <param name="item">The item to update.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Update(T item);
    }
}
