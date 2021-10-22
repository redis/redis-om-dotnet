using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM;
using StackExchange.Redis;

namespace Redis.OM
{
    public class XPendingMessage
    {
        public string MessageId { get; set; }
        public string ConsumerName { get; set; }
        public long MillisecondsSinceLastDelivery { get; set; }
        public int NumberOfDeliveries { get; set; }
        public XPendingMessage(RedisReply[] values)
        {
            MessageId = values[0];
            ConsumerName = values[1];
            MillisecondsSinceLastDelivery = (int)values[2];
            NumberOfDeliveries = (int)values[3];
        }

        public XPendingMessage(StreamPendingMessageInfo info)
        {
            MessageId = info.MessageId;
            ConsumerName = info.ConsumerName;
            MillisecondsSinceLastDelivery = info.IdleTimeInMilliseconds;
            NumberOfDeliveries = info.DeliveryCount;
        }
    }
}
