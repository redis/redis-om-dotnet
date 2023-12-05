using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM;

namespace Redis.OM
{
    public class XRangeResponse<T>
        where T : notnull
    {
        public IDictionary<string,T> Messages { get; set; }

        public XRangeResponse(RedisResult[] vals, string streamName)
        {
            Messages = new Dictionary<string, T>();
            if (!vals.Any())
                return;
            var index = 0;
            var arr = ((RedisResult[])vals[0]);
            for (index = 0; index < ((RedisResult[])vals[0]).Length; index++)
            {
                if (((string)arr[index]) == streamName)
                {
                    index += 1;
                    break;
                }
            }

            var obj = ((RedisResult[])arr[index]);
            for (var i = 0; i < obj.Length; i += 2)
            {
                var id = (string)((RedisResult[])obj.ToArray()[0])[i];
                var pairs = ((RedisResult[])((RedisResult[])obj.ToArray()[0])[i + 1]);
                var messageDict = new Dictionary<string, RedisReply>();
                for (var j = 0; j < pairs.Length; j += 2)
                {
                    messageDict.Add(((string)pairs[j]), new RedisReply(pairs[j + 1]));
                }
                Messages.Add(id, (T)RedisObjectHandler.FromHashSet<T>(messageDict));
            }
        }
        public XRangeResponse(RedisReply[] vals, string streamName) 
        {
            Messages = new Dictionary<string, T>();
            if (!vals.Any())
                return;
            var index = 0;
            for(index = 0; index < vals[0].ToArray().Length; index++)
            {
                if(vals[0].ToArray()[index] == streamName)
                {
                    index += 1;
                    break;
                }
            }
            var obj = vals[0].ToArray()[index].ToArray();
            for (var i = 0; i < obj.Length; i += 2)
            {
                var id = (string)obj.ToArray()[0].ToArray()[i];
                var pairs = obj.ToArray()[0].ToArray()[i + 1].ToArray();
                var messageDict = new Dictionary<string, RedisReply>();
                for (var j = 0; j < pairs.Length; j+=2)
                {
                    messageDict.Add(pairs[j], pairs[j + 1]);
                }
                Messages.Add(id, (T)RedisObjectHandler.FromHashSet<T>(messageDict));
            }
        }
    }
    public class XRangeResponse
    {
        public IDictionary<string, IDictionary<string, string>> Messages { get; set; }

        public XRangeResponse(StreamEntry[] entries)
        {
            Messages = new Dictionary<string, IDictionary<string, string>>();

            foreach(var entry in entries)
            {
                var innerDict = entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
                Messages.Add(entry.Id, innerDict);
            }
        }

        public XRangeResponse(RedisReply[] vals)
        {
            Messages = new Dictionary<string, IDictionary<string, string>>();
            for (var i = 1; i < vals.Length; i += 2)
            {
                var id = (string)vals[i];
                var pairs = vals[i + 1].ToArray();
                var messageDict = new Dictionary<string, string>();
                for (var j = 0; j < pairs.Length; j++)
                {
                    messageDict.Add(pairs[j], pairs[j + 1]);
                }
                Messages.Add(id, messageDict);
            }
        }

        public XRangeResponse(RedisResult[] vals)
        {
            Messages = new Dictionary<string, IDictionary<string,string>>();
            if (!vals.Any())
                return;

            for (var i = 1; i < vals.Length; i += 2)
            {
                var id = (string)vals[i];
                var pairs = ((RedisResult[])vals[i + 1]);
                var messageDict = new Dictionary<string, string>();
                for (var j = 0; j < pairs.Length; j++)
                {
                    messageDict.Add(pairs[j].ToString(), pairs[j + 1].ToString());
                }
                Messages.Add(id, messageDict);
            }            
        }
    }
}
