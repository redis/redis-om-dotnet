using System;
using System.Threading.Tasks;

namespace Redis.OM.Contracts
{
    /// <summary>
    /// A connection to Redis.
    /// </summary>
    public interface IRedisConnection : IDisposable
    {
        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The command name.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>A redis Reply.</returns>
        RedisReply Execute(string command, params object[] args);

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The command name.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>A redis Reply.</returns>
        Task<RedisReply> ExecuteAsync(string command, params object[] args);

        /// <summary>
        /// Executes the contained commands within the context of a transaction.
        /// </summary>
        /// <param name="commandArgsTuples">each tuple represents a command and
        ///     it's arguments to execute inside a transaction.</param>
        /// <returns>A redis Reply.</returns>
        Task<RedisReply[]> ExecuteInTransactionAsync(Tuple<string, object[]>[] commandArgsTuples);

        /// <summary>
        /// Executes the contained commands within the context of a transaction.
        /// </summary>
        /// <param name="commandArgsTuples">each tuple represents a command and
        ///     it's arguments to execute inside a transaction.</param>
        /// <returns>A redis Reply.</returns>
        RedisReply[] ExecuteInTransaction(Tuple<string, object[]>[] commandArgsTuples);
    }
}
