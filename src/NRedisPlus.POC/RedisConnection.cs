using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;
using NRedisPlus.RediSearch;
using Newtonsoft.Json;

namespace NRedisPlus
{
    public partial class RedisConnection : IRedisConnection, IDisposable
    {
        private Socket _socket;
        private TcpClient _tcpClient;
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
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

        public RedisReply Execute(string command, params string[] args)
        {
            var commandBytes = RespHelper.BuildCommand(command, args);
            _socket.Send(commandBytes);
            return RespHelper.GetNextReplyFromSocket(_socket);
        }

        public async Task<RedisReply> ExecuteAsync(string command, params string[] args)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var commandBytes = new ArraySegment<byte>(RespHelper.BuildCommand(command, args));
                await _socket.SendAsync(commandBytes, SocketFlags.None);                
                return await RespHelper.GetNextReplyFromSocketAsync(_socket);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            
        }

    }
}
