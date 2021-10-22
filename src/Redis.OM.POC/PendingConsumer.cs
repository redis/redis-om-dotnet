using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM;

namespace Redis.OM
{
    public class PendingConsumer
    {
        public string ConsumerName { get; set; }
        public int NumPendingMessages { get; set; }
        
        public PendingConsumer(RedisReply value)
        {
            var vals = value.ToArray();
            ConsumerName = vals[0];
            NumPendingMessages = (int)vals[1];
        }

        internal PendingConsumer(StreamConsumer consumer)
        {
            ConsumerName = consumer.Name;
            NumPendingMessages = consumer.PendingMessageCount;
        }
    }
}
