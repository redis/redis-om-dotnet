using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NRedisPlus.Contracts;

namespace NRedisPlus
{
    public class RedisStream<T> : IAsyncEnumerable<T> 
        where T : notnull, new()
    {
        private string _streamKeyName;
        private IRedisConnection _connection;       
        internal string CurrentId { get; set; }
        private int _chunkSize;
        private string _groupName;
        private string _consumerName;
        public RedisStream(string keyName, IRedisConnection connection, string currentId = "$", int chunkSize = 100, string groupName="", string consumerName="")
        {
            _streamKeyName = keyName;
            _connection = connection;
            CurrentId = currentId;
            _chunkSize = chunkSize;
            _groupName = groupName;
            _consumerName = consumerName;
            
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new StreamEnumorator<T>(this, _connection, _streamKeyName, _groupName, cancellationToken, _consumerName);
        }

        public async Task<string?> Add(T obj)
        {
            return await _connection.XAddAsync(_streamKeyName, obj);
        }
    }
}
