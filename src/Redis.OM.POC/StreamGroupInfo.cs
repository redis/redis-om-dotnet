using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Redis.OM.Modeling;

namespace Redis.OM
{
    public class StreamGroupInfo
    {
        [RedisField(PropertyName = "name")]
        public string Name { get; set; }
        [RedisField(PropertyName = "consumers")]
        public int Consumers { get; set; }
        [RedisField(PropertyName = "pending")]
        public int Pending { get; set; }
        [RedisField(PropertyName = "last-delivered-id")]
        [JsonPropertyName("last-delivered-id")]
        public string LastDeliveredId { get; set; }

        internal StreamGroupInfo(StackExchange.Redis.StreamGroupInfo info)
        {
            Name = info.Name;
            Consumers = info.ConsumerCount;
            Pending = info.PendingMessageCount;
            LastDeliveredId = info.LastDeliveredId;
        }
    }
}
