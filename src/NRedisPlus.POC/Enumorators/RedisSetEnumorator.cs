using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRedisPlus.Contracts;

namespace NRedisPlus
{
    public class RedisSetEnumorator : CursorEnumeratorBase<string>
    {
        public RedisSetEnumorator(IRedisConnection connection, string keyName, uint chunkSize = 100) :
            base(connection, keyName, chunkSize)
        {
        }
        protected override void GetNextChunk()
        {
            _chunk = _connection.SScan(_keyName, ref _cursor, count: _chunkSize).ToArray();
            _chunkIndex = 0;
        }
    }
}
