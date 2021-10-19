using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NRedisPlus
{
    public class StreamInfoFull
    {
        public KeyValuePair<string, IDictionary<string, string>> FirstEntry {get; private set;}
        public KeyValuePair<string, IDictionary<string, string>> LastEntry {get; private set;}
        public long RadixTreeNodes { get; private set; }
        public long RadixTreeKeys { get; private set; }
        public long Length { get; private set; }

        public StreamInfoFull(StreamInfo info)
        {
            Length = info.Length;
            RadixTreeKeys = info.RadixTreeKeys;
            RadixTreeNodes = info.RadixTreeNodes;
            FirstEntry = new KeyValuePair<string, IDictionary<string, string>>(
                info.FirstEntry.Id, 
                info.FirstEntry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString()));
            LastEntry = new KeyValuePair<string, IDictionary<string, string>>(
                info.LastEntry.Id,
                info.LastEntry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString()));
        }
    }
}
