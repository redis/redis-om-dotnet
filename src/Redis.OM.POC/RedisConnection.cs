using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Newtonsoft.Json;
using Redis.OM.Contracts;
using Redis.OM;
using StackExchange.Redis;

namespace Redis.OM
{
    public partial class RedisConnection : IRedisConnection, IDisposable
    {
        private Socket _socket;
        private TcpClient _tcpClient;
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly IDatabase _db;

        //private static var tran = IDatabase.CreateTransaction();
        public RedisConnection(string hostName="localhost")
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _tcpClient = new TcpClient(hostName, 6379);
            var entry = Dns.GetHostEntry(hostName);
            if (entry.AddressList.Length > 0)
            {
                _socket.Connect(entry.AddressList[1], 6379);
            }
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public RedisList GetList(string listName, uint chunkSize = 100)
        {
            return new RedisList(this, listName, chunkSize);
        }

        public RedisReply Execute(string command, params object[] args)
        {
            var commandBytes = RespHelper.BuildCommand(command, args.Select(x=>x.ToString()).ToArray());
            _socket.Send(commandBytes);
            return RespHelper.GetNextReplyFromSocket(_socket);
        }

        public async Task<RedisReply> ExecuteAsync(string command, params object[] args)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var commandBytes = new ArraySegment<byte>(RespHelper.BuildCommand(command, args.Select(x=>x.ToString()).ToArray()));
                await _socket.SendAsync(commandBytes, SocketFlags.None);
                return await RespHelper.GetNextReplyFromSocketAsync(_socket);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

        }

        /// <inheritdoc/>
        public RedisReply[] ExecuteInTransaction(Tuple<string, object[]>[] commandArgsTuples)
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
        public async Task<RedisReply[]> ExecuteInTransactionAsync(Tuple<string, object[]>[] commandArgsTuples)
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
    }
}
