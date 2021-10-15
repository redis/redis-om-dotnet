using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NRedisPlus
{
    public class XAutoClaimResponse<T> : XRangeResponse<T>
        where T : notnull
    {
        public string NextId { get; set; }
        //public IDictionary<string,T> Messages { get; set; }
        public XAutoClaimResponse(RedisReply[] vals, string id) : base(vals.Skip(1).ToArray(), id)
        {            
            NextId = vals[0];            
        }

        public XAutoClaimResponse(RedisResult[] vals, string id) : base(vals.Skip(1).ToArray(), id)
        {
            NextId = ((string)vals[0]);
        }
    }

    public class XAutoClaimResponse : XRangeResponse
    {
        public string NextId { get; set; }        
        public XAutoClaimResponse(RedisReply[] vals) :base(vals.Skip(1).ToArray())
        {
            NextId = vals[0];            
        }

        public XAutoClaimResponse(RedisResult[] vals) : base(vals.Skip(1).ToArray())
        {
            NextId = vals[0].ToString();
        }
    }
}
