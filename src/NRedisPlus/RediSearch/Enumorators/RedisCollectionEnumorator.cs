using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace NRedisPlus.RediSearch.Enumorators
{
    internal class RedisCollectionEnumorator<T> : IEnumerator<T>, IAsyncEnumerator<T> where T : notnull
    {
        private RedisQuery _query;
        private Type _rootType;
        private Type? _primitiveType;
        private bool _limited;
        private SearchResponse<T> _records = new SearchResponse<T>(new RedisReply(new RedisReply[] { 0 }));
        private bool _started = false;
        private int _index = -1;
        private IRedisConnection _connection;
        private RedisCollectionStateManager _stateManager;
        private DocumentAttribute _documentDefinition;

        public RedisCollectionEnumorator(Expression exp, IRedisConnection connection, int chunkSize, RedisCollectionStateManager stateManager)
        {
            var t = typeof(T);
            _documentDefinition = t.GetCustomAttribute<DocumentAttribute>();
            if(_documentDefinition == null)
            {
                _primitiveType = t;
                _rootType = GetRootType((MethodCallExpression)exp);
                _documentDefinition = _rootType.GetCustomAttribute<DocumentAttribute>();
            }
            else
            {
                _rootType = t;
            }
            _query = ExpressionTranslator.BuildQueryFromExpression(exp, _rootType);            
            if (_query.Limit != null)
            {
                _limited = true;
                
            }
            else
            {
                _query.Limit = new SearchLimit { Offset = 0, Number = chunkSize };
            }
            _connection = connection;
            _stateManager = stateManager;
        }        

        public T Current => _records[_index];

        object IEnumerator.Current => _records[_index];

        public void Dispose() {}

        public async ValueTask DisposeAsync()
        {            
        }

        private bool GetNextChunk()
        {
            if (_query.Limit == null)
                throw new ArgumentNullException("Query Limit cannot be null");
            if(_started)
                _query.Limit.Offset = _query.Limit.Offset + _query.Limit.Number;            
            var res = _connection.Search<T>(_query);            
            _records = new SearchResponse<T>(res);            
            _index = 0;
            _started = true;
            ConcatenateRecords();
            return (_index < _records.Documents.Count);
        }        

        private async ValueTask<bool> GetNextChunkAsync()
        {
            if (_query.Limit == null)
                throw new ArgumentNullException("Query Limit cannot be null");
            if(_started)
                _query.Limit.Offset = _query.Limit.Offset + _query.Limit.Number;            
            _records = await _connection.SearchAsync<T>(_query);
            _index = 0;
            _started = true;
            ConcatenateRecords();
            return (_index < _records.Documents.Count);
        }

        private void ConcatenateRecords()
        {
            foreach(var record in _records.Documents)
            {
                if (!_stateManager.Data.ContainsKey(record.Key) && _primitiveType == null)
                {
                    _stateManager.Data.Add(record.Key, record.Value);
                    _stateManager.InsertIntoSnapshot(record.Key, record.Value);
                }       
            }
        }

        public bool MoveNext()
        {
            if (_index + 1 < _records.Documents.Count)
            {
                _index += 1;
                return true;
            }                
            if (_started && _limited)
                return false;
            if (_started && _records.Documents.Count < _query.Limit.Number)
                return false;
            return GetNextChunk();
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_index + 1 < _records.Documents.Count)
            {
                _index += 1;
                return true;
            }                
            if (_started && _limited)
                return false;
            if (_started && _records.Documents.Count < _query.Limit.Number)
                return false;
            return await GetNextChunkAsync();
        }

        public void Reset()
        {
            _started = false;
            _index = -1;
            _records = new SearchResponse<T>(new RedisReply(new RedisReply[] { 0 }));
            if(_query.Limit != null)
                _query.Limit.Offset = 0;
        }

        private static Type GetRootType(MethodCallExpression expression)
        {
            while (expression.Arguments[0] is MethodCallExpression innerExpression)
                expression = innerExpression;
            return expression.Arguments[0].Type.GenericTypeArguments[0];
        }
    }
}
