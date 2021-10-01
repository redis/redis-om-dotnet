using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{
    public abstract class CursorEnumeratorBase<T> : RedisEnumoratorBase<T>
        where T : notnull
    {
        public CursorEnumeratorBase(IRedisConnection connection, string keyName, uint chunkSize = 100) : base(connection, keyName, chunkSize) { }

        public override bool MoveNext()
        {
            if(_chunkIndex == -1)
            {
                GetNextChunk();
                return _chunkIndex < _chunk.Length;
            }
            _chunkIndex++;
            if(_chunkIndex < _chunk.Length)
            {
                return true;
            }
            else if (_cursor != 0)
            {
                GetNextChunk();
                return _chunkIndex < _chunk.Length;
            }
            else
            {
                return false;
            }
        }
    }
}
