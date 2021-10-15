using System;
using System.Threading.Tasks;

namespace NRedisPlus.Contracts
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
        RedisReply Execute(string command, params string[] args);

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The command name.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>A redis Reply.</returns>
        Task<RedisReply> ExecuteAsync(string command, params string[] args);
    }
}
