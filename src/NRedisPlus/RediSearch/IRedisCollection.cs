using System.Linq;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    /// <summary>
    /// A collection of items in redis that you can use to look up items in redis, or perform queries on indexed documents.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    public interface IRedisCollection<T> : IOrderedQueryable<T>
    {
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
    }
}
