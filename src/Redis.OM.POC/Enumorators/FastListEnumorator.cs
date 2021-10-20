using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;

namespace Redis.OM
{
    public class FastListEnumorator : CursorEnumeratorBase<string>
    {
        public FastListEnumorator(IRedisConnection connection, string keyName, uint chunkSize = 100) : 
            base(connection, keyName, chunkSize)
        {
        }

        protected override void GetNextChunk()
        {
            _chunk = _connection.ZScan(_keyName, ref _cursor, count: _chunkSize).Select(m => m.Member).ToArray();
                //.Select(s=>s?.Member?.Substring(0,s.Member.Length-(s.Member.Length-s.Member.LastIndexOf('-')))).ToArray();
            _chunkIndex = 0;
        }
    }
}
