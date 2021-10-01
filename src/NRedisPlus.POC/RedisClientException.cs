using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{
    public class RedisClientException : Exception
    {
        public RedisClientException(string message) : base(message) { }
    }
}
