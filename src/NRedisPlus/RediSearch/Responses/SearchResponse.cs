using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NRedisPlus.RediSearch
{
    public class SearchResponse
    {
        public long DocumentCount { get; set; }
        public IDictionary<string,IDictionary<string,string>> Documents { get; set; }

        public SearchResponse(RedisResult val)
        {
            var vals = (RedisResult[])val;
            DocumentCount = ((long)vals[0]);
            Documents = new Dictionary<string, IDictionary<string,string>>();
            for (var i = 1; i < vals.Count(); i += 2)
            {
                var docId = (string)vals[i];
                var documentHash = new Dictionary<string, string>();
                var docArray = ((RedisValue[])vals[i + 1]);
                for (var j = 0; j < docArray.Length; j += 2)
                {
                    documentHash.Add(docArray[j], docArray[j + 1]);
                }                
                Documents.Add(docId, documentHash);
            }
        }

        public SearchResponse(RedisReply val)
        {
            var vals = val.ToArray();
            DocumentCount = vals[0];
            Documents = new Dictionary<string, IDictionary<string,string>>();
            for (var i = 1; i < vals.Count(); i += 2)
            {
                var docId = (string)vals[i];
                var documentHash = new Dictionary<string, string>();
                var docArray = vals[i + 1].ToArray();
                for (var j = 0; j < docArray.Length; j += 2)
                {
                    documentHash.Add(docArray[j], docArray[j + 1]);
                }                
                Documents.Add(docId, documentHash);
            }
        }

        public IDictionary<string,T> DocumentsAs<T>()
            where T : notnull
        {
            var dict = new Dictionary<string, T>();
            foreach(var kvp in Documents)
            {
                var obj = (T)RedisObjectHandler.FromHashSet<T>(kvp.Value);
                dict.Add(kvp.Key, obj);
            }
            return dict;
        }
    }

    public class SearchResponse<T>
        where T : notnull
    {
        public long DocumentCount { get; set; }
        public IDictionary<string, T> Documents { get; set; }
        public T this[string key] { get => Documents[key]; }
        internal T this[int index] { get => Documents.Values.ElementAt(index); }
        
        private SearchResponse()
        {
            DocumentCount = 0;
            Documents = new Dictionary<string, T>();
        }

        public SearchResponse(RedisResult val)
        {
            var vals = (RedisResult[])val;
            DocumentCount = ((long)vals[0]);
            Documents = new Dictionary<string, T>();
            for(var i = 1; i < vals.Count(); i += 2)
            {
                var docId = (string)vals[i];
                var documentHash = new Dictionary<string, string>();
                var docArray = ((RedisValue[])vals[i + 1]);
                for(var j = 0; j < docArray.Length; j += 2)
                {
                    documentHash.Add(docArray[j], docArray[j + 1]);
                }
                var obj = (T)RedisObjectHandler.FromHashSet<T>(documentHash);
                Documents.Add(docId, obj);
            }
        }

        public SearchResponse(RedisReply val)
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type);
            if((type.IsPrimitive || type == typeof(string))) 
            {
                var @this = PrimitiveSearchResponse(val);
                Documents = @this.Documents;
                DocumentCount = @this.DocumentCount;
            }
            else if (underlyingType is {IsPrimitive: true})
            {
                var @this = PrimitiveSearchResponse(val);
                Documents = @this.Documents;
                DocumentCount = @this.DocumentCount;
            }
            else
            {
                var vals = val.ToArray();
                DocumentCount = vals[0];
                Documents = new Dictionary<string, T>();
                for (var i = 1; i < vals.Count(); i += 2)
                {
                    var docId = (string)vals[i];
                    var documentHash = new Dictionary<string, string>();
                    var docArray = vals[i + 1].ToArray();
                    for (var j = 0; j < docArray.Length; j += 2)
                    {
                        documentHash.Add(docArray[j], docArray[j + 1]);
                    }
                    var obj = RedisObjectHandler.FromHashSet<T>(documentHash);
                    Documents.Add(docId, obj);
                }
            }            
        }

        private static SearchResponse<T> PrimitiveSearchResponse(RedisReply redisReply)
        {
            var arr = redisReply.ToArray();
            var response = new SearchResponse<T>();
            response.DocumentCount = arr[0];
            for(var i = 1; i < arr.Count(); i += 2)
            {
                var docId = (string)arr[i];
                var primitive = (T)Convert.ChangeType(arr[i + 1].ToArray()[1], typeof(T));
                response.Documents.Add(docId, primitive);
            }
            return response;
        }

    }
}
