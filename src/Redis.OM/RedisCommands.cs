using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using StackExchange.Redis;

namespace Redis.OM
{
    /// <summary>
    /// A connection to redis.
    /// </summary>
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
            try
            {
                var result = _db.Execute(command, args);
                return new RedisReply(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed on {command} {string.Join(" ", args)}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<RedisReply> ExecuteAsync(string command, params string[] args)
        {
            try
            {
                var result = await _db.ExecuteAsync(command, args);
                return new RedisReply(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed on {command} {string.Join(" ", args)}", ex);
            }
        }

        /// <inheritdoc/>
        public RedisReply[] ExecuteInTransaction(Tuple<string, string[]>[] commandArgsTuples)
        {
            var transaction = _db.CreateTransaction();
            var tasks = new List<Task<RedisResult>>();
            foreach (var tuple in commandArgsTuples)
            {
                tasks.Add(transaction.ExecuteAsync(tuple.Item1, tuple.Item2));
            }

            transaction.Execute();
            Task.WhenAll(tasks).Wait();
            return tasks.Select(x => new RedisReply(x.Result)).ToArray();
        }

        /// <inheritdoc/>
        public async Task<RedisReply[]> ExecuteInTransactionAsync(Tuple<string, string[]>[] commandArgsTuples)
        {
            var transaction = _db.CreateTransaction();
            var tasks = new List<Task<RedisResult>>();
            foreach (var tuple in commandArgsTuples)
            {
                tasks.Add(transaction.ExecuteAsync(tuple.Item1, tuple.Item2));
            }

            await transaction.ExecuteAsync();
            await Task.WhenAll(tasks);
            return tasks.Select(x => new RedisReply(x.Result)).ToArray();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
