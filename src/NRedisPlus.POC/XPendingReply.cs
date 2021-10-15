using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{
    public class XPendingReply
    {
        public int NumPendingMessages { get; private set; }
        public string SmallestPendingId { get; private set; }
        public string LargestPendingId { get; private set; }
        public IList<PendingConsumer> PendingConsumers { get; private set; }

        public XPendingReply(RedisReply value)
        {
            var vals = value.ToArray();
            NumPendingMessages = (int)vals[0];
            SmallestPendingId = vals[1];
            LargestPendingId = vals[2];
            PendingConsumers = new List<PendingConsumer>();
            foreach(var val in vals.Skip(3).ToArray())
            {
                PendingConsumers.Add(new PendingConsumer(val));
            }
        }

        internal XPendingReply(StreamPendingInfo info)
        {
            LargestPendingId = info.HighestPendingMessageId;
            SmallestPendingId = info.LowestPendingMessageId;
            NumPendingMessages = info.PendingMessageCount;
            PendingConsumers = new List<PendingConsumer>();
            foreach(var consumer in info.Consumers)
            {
                PendingConsumers.Add(new PendingConsumer(consumer));
            }
        }
    }
}
