using System.Collections;
using System.Collections.Generic;
using NRedisPlus.Contracts;

namespace NRedisPlus
{
    public abstract class RedisEnumoratorBase<T> : IEnumerator<T>
        where T : notnull
    {
        protected T[] _chunk;
        protected int _chunkIndex = -1;
        protected int _cursor = 0;
        protected int _offset = 0;
        protected readonly uint _chunkSize = 100;
        protected IRedisConnection _connection;
        protected readonly string _keyName;

        public RedisEnumoratorBase(IRedisConnection connection, string keyName, uint chunkSize = 100)
        {
            _connection = connection;
            _keyName = keyName;
            _chunkSize = chunkSize;
            _chunk = new T[0];
        }

        protected abstract void GetNextChunk();

        public T Current => _chunk[_chunkIndex];

        object IEnumerator.Current => _chunk[_chunkIndex];

        public void Dispose()
        {
            //do nothing
        }

        public virtual bool MoveNext()
        {
            if (_chunkIndex == -1 || _chunkIndex + 1 >= _chunk.Length)
            {
                GetNextChunk();
                return _chunkIndex < _chunk.Length;
            }
            _chunkIndex++;
            return _chunkIndex < _chunk.Length;
        }

        public void Reset()
        {
            _cursor = 0;
            _offset = 0;
            GetNextChunk();
        }
    }
}
