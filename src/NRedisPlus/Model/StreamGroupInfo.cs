using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NRedisPlus.RediSearch;

namespace NRedisPlus
{
    public class StreamGroupInfo
    {
        [RedisField("name")]
        public string Name { get; set; }
        [RedisField("consumers")]
        public int Consumers { get; set; }
        [RedisField("pending")]
        public int Pending { get; set; }
        [RedisField("last-delivered-id")]
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
