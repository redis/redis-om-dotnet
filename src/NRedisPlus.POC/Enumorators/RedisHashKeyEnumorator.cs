using System.Linq;

namespace NRedisPlus
{
    public class RedisHashKeyEnumorator : CursorEnumeratorBase<string>
    {
        public RedisHashKeyEnumorator(IRedisConnection connection, string keyName, uint chunkSize = 100) :
            base(connection, keyName, chunkSize)
        {
        }

        protected override void GetNextChunk()
        {
            _chunk = _connection.HScan(_keyName, ref _cursor, count: _chunkSize).Select(kvp => kvp.Key).ToArray();
            _chunkIndex = 0;
        }
    }
}
