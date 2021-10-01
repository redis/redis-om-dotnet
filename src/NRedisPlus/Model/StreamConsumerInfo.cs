using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
namespace NRedisPlus
{
    public class StreamConsumerInfo
    {
        public string Name { get; private set; }
        public int Pending { get; private set; }
        public long Idle { get; private set; }

        public StreamConsumerInfo(StackExchange.Redis.StreamConsumerInfo consumer)
        {
            Name = consumer.Name;
            Pending = consumer.PendingMessageCount;
            Idle = consumer.IdleTimeInMilliseconds;
        }
    }
}
