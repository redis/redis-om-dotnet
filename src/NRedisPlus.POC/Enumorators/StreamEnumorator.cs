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
    public class StreamEnumorator<T> : IAsyncEnumerator<T> 
        where T : notnull, new()
    {
        private RedisStream<T> _parentStream;
        private string _previousId = string.Empty;
        private IRedisConnection _connection;
        private T? _currentItem;
        private string _streamId;
        private string _groupName;
        private CancellationToken _token;
        private string _consumerName;
        private bool _groupEstablished = false;
        public StreamEnumorator(RedisStream<T> parentStream, IRedisConnection connection, string streamId, string groupName, CancellationToken token, string consumerName)
        {
            _connection = connection;
            _parentStream = parentStream;
            _streamId = streamId;
            _groupName = groupName;
            _token = token;
            _previousId = parentStream.CurrentId;
            _consumerName = consumerName;
        }

        public T Current => _currentItem ?? throw new NullReferenceException();

        public ValueTask DisposeAsync()
        {
            //do nothing
            return new ValueTask();            
        }

        private async ValueTask EstablishGroup()
        {
            var groups = await _connection.XInfoGroupsAsync(_streamId);
            if(!groups.Any(g=>g.Name == _groupName))
            {
                await _connection.XGroupCreateGroupAsync(_streamId, _groupName, _previousId, true);
            }
            _groupEstablished = true;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_token.IsCancellationRequested)
            {                
                return false;
            }
            if (!string.IsNullOrEmpty(_groupName) && !_groupEstablished)
            {
                await EstablishGroup();
            }
                
            if (!string.IsNullOrEmpty(_previousId) && _previousId != "$" &&  !string.IsNullOrEmpty(_groupName))
            {
                _connection.XAck(_streamId, _groupName, _previousId);
            }
            while (true)
            {
                if (_token.IsCancellationRequested)
                {
                    return false;
                }
                XRangeResponse<T> nextItem;
                if (!string.IsNullOrEmpty(_groupName))
                {
                    nextItem = await _connection.XReadGroupAsync<T>(_streamId, ">", _groupName, _consumerName, blockMs:5000, count: 1);
                }
                else
                {
                    nextItem = await _connection.XReadAsync<T>(_streamId, _previousId, blockMilliseconds: 5000, count: 1);
                }
                
                if (nextItem.Messages.Any())
                {                    
                    _currentItem = nextItem.Messages.FirstOrDefault().Value;
                    _previousId = nextItem.Messages.FirstOrDefault().Key;
                    return true;
                }                
            }            
        }
    }
}
