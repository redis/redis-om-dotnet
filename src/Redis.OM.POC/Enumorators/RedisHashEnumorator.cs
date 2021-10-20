using System.Collections.Generic;
using Redis.OM.Contracts;

namespace Redis.OM
{
    public class RedisHashEnumorator : RedisEnumoratorBase<KeyValuePair<string, string>>
    {
        public RedisHashEnumorator(IRedisConnection connection, string keyName, uint chunkSize = 100) : 
            base(connection, keyName, chunkSize)
        {
        }

        protected override void GetNextChunk()
        {
            _chunk = _connection.HScan(_keyName, ref _cursor, count: _chunkSize).ToArray();
            _chunkIndex = -1;
        }
    }
}
