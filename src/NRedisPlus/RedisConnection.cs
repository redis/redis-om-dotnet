using System;
using System.Threading.Tasks;
using NRedisPlus.Contracts;
using StackExchange.Redis;

namespace NRedisPlus
{
    internal class RedisConnection : IRedisConnection
    {
        private readonly IDatabase _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnection"/> class.
        /// </summary>
        /// <param name="db">StackExchange.Redis IDatabase object.</param>
        internal RedisConnection(IDatabase db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public RedisReply Execute(string command, params string[] args)
        {
            var result = _db.Execute(command, args);
            return new RedisReply(result);
        }

        /// <inheritdoc/>
        public async Task<RedisReply> ExecuteAsync(string command, params string[] args)
        {
            var result = await _db.ExecuteAsync(command, args);
            return new RedisReply(result);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
