using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;

namespace Redis.OM.Enumorators
{
    public class SortedSetEnumorator : CursorEnumeratorBase<string>
    {
        public SortedSetEnumorator(IRedisConnection connection, string keyName, uint chunkSize)
            : base(connection, keyName, chunkSize) { }
        protected override void GetNextChunk()
        {
            _connection.ZScan(_keyName, ref _cursor, count: _chunkSize);
            _chunkIndex = 0;
        }
    }
}
