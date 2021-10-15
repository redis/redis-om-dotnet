using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NRedisPlus.Contracts;

namespace NRedisPlus.RediSearch
{
    public class AggregationEnumorator<T> : IEnumerator<AggregationResult<T>>, IAsyncEnumerator<AggregationResult<T>>
    {
        private IRedisConnection _connection;        
        private AggregationResult<T>[] _chunk;
        private int _index;
        private int _chunkSize;
        private int _cursor = -1;
        private RedisAggregation _aggregation;
        private bool _useCursor;
        private bool _queried = false;

        internal AggregationEnumorator(Expression exp, IRedisConnection connection, int chunkSize = 1000, bool useCursor = false)
        {   
            _chunk = new AggregationResult<T>[0];
            _index = 0;
            _chunkSize = 1000;
            _aggregation = ExpressionTranslator.BuildAggregationFromExpression(exp, typeof(T));
            _connection = connection;
            _useCursor = useCursor;
        }

        public AggregationResult<T> Current => _chunk[_index];

        object IEnumerator.Current => _chunk[_index];

        public void Dispose()
        {
            if (_cursor != 0)
            {
                try
                {                    
                    var args = new[] { "DEL", _aggregation.IndexName, _cursor.ToString() };
                    _connection.Execute("FT.CURSOR", args);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Unable to delete cursor, most likely because it had already been exhausted");
                    System.Diagnostics.Trace.WriteLine(ex);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if(_cursor != 0)
            {
                try
                {
                    var args = new[] { "DEL", _aggregation.IndexName, _cursor.ToString() };
                    await _connection.ExecuteAsync("FT.CURSOR", args);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                }
            }
        }

        public bool MoveNext()
        {
            if (_index + 1 < _chunk.Length)
            {
                _index++;
                return true;
            }
            if (_useCursor)
            {
                if (_cursor == -1)
                {
                    return StartEnumeration();
                }
                else if (_cursor == 0)
                {
                    return false;
                }
                else
                {
                    return ReadNextChunk();
                }
            }            
            else
            {
                if (!_queried)
                    return StartEnumeration();
                else
                    return false;
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_index + 1 < _chunk.Length)
            {
                _index++;
                return true;
            }
            if (_useCursor)
            {
                if (_cursor == -1)
                {
                    return await StartEnumerationAsync();
                }
                else if (_cursor == 0)
                {
                    return false;
                }
                else
                {
                    return await ReadNextChunkAsync();
                }
            }
            else
            {
                if (!_queried)
                    return await StartEnumerationAsync();
                else
                    return false;
            }
            
        }

        public void Reset()
        {
            _cursor = -1;
            _index = 0;
            _chunk = new AggregationResult<T>[0];
        }

        private string[] NextChunkArgs { get => new[]
            {
                "READ",
                _aggregation.IndexName,
                _cursor.ToString(),
                "COUNT",
                _chunkSize.ToString()
            };
        }

        private string[] SerializedArgs { 
            get
            {
                var serializedArgs = _aggregation.Serialize().ToList();
                if (_useCursor)
                {
                    serializedArgs.Add("WITHCURSOR");
                    serializedArgs.Add("COUNT");
                    serializedArgs.Add(_chunkSize.ToString());
                }                
                return serializedArgs.ToArray();
            } 
        }

        private void ParseResult(RedisReply res)
        {
            
            if(_useCursor)
            {
                var arr = res.ToArray();
                _cursor = arr[1];
                _chunk = AggregationResult<T>.FromRedisResult(arr[0]).ToArray();
            }
            else
            {
                _chunk = AggregationResult<T>.FromRedisResult(res).ToArray();
            }
            _index = 0;
            _queried = true;
        }


        protected async ValueTask<bool> ReadNextChunkAsync()
        {
            var res = await _connection.ExecuteAsync("FT.CURSOR", NextChunkArgs);
            ParseResult(res);
            return _index < _chunk.Length;
        }

        protected async ValueTask<bool> StartEnumerationAsync()
        {
            var res = await _connection.ExecuteAsync("FT.AGGREGATE", SerializedArgs);
            ParseResult(res);
            return _index < _chunk.Length;
        }

        protected bool ReadNextChunk()
        {
            var res = _connection.Execute("FT.CURSOR", NextChunkArgs);
            ParseResult(res);
            return _index < _chunk.Length;
        }

        protected bool StartEnumeration()
        {
            var res = _connection.Execute("FT.AGGREGATE", SerializedArgs.ToArray());
            ParseResult(res);
            return _index < _chunk.Length;
        }
    }
}
