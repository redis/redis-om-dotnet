using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NRedisPlus.Contracts;

namespace NRedisPlus
{
    public static class RedisCommands
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        static RedisCommands()
        {
            _options.Converters.Add(new RediSearch.GeoLocJsonConverter());
        }
        
        public static async Task<string> SetAsync(this IRedisConnection connection, object obj)
        {
            var id = obj.SetId();
            var type = obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if(attr == null || attr.StorageType == StorageType.HASH)
            {
                if (obj is IRedisHydrateable hydrateable)
                {
                    await connection.HSetAsync(id, hydrateable.BuildHashSet().ToArray());
                }
                else
                {
                    await connection.HSetAsync(id, obj.BuildHashSet().ToArray());
                }
            }
            else
            {
                await connection.JsonSetAsync(id, ".", obj);
            }
            return id;
        }
        
        public static async Task<int> HSetAsync(this IRedisConnection connection, string key, params KeyValuePair<string,string>[] fieldValues)
        {
            var args = new List<string>();
            args.Add(key);
            foreach(var kvp in fieldValues)
            {
                args.Add(kvp.Key);
                args.Add(kvp.Value);
            }
            return (await connection.ExecuteAsync("HSET", args.ToArray()));            
        }
        
        public static async Task<bool> JsonSetAsync(this IRedisConnection connection, string key, string path, string json)
        {
            var result = await connection.ExecuteAsync("JSON.SET", key, path, json);
            return ((string)result) == "OK";
        }

        public static async Task<bool> JsonSetAsync(this IRedisConnection connection, string key, string path, object obj)
        {            
            var json = JsonSerializer.Serialize(obj, _options);
            var result = await connection.ExecuteAsync("JSON.SET", key, path, json);
            return ((string)result) == "OK";
        }
        
        public static int HSet(this IRedisConnection connection, string key, params KeyValuePair<string,string>[] fieldValues)
        {
            var args = new List<string>();
            args.Add(key);
            foreach(var kvp in fieldValues)
            {
                args.Add(kvp.Key);
                args.Add(kvp.Value);
            }
            return (int)connection.Execute("HSET", args.ToArray());            
        }
        
        public static bool JsonSet(this IRedisConnection connection, string key, string path, string json)
        {
            var result = connection.Execute("JSON.SET", key, path, json);
            return ((string)result) == "OK";
        }
        public static bool JsonSet(this IRedisConnection connection, string key, string path, object obj)
        {            
            var json = JsonSerializer.Serialize(obj, _options);
            var result = connection.Execute("JSON.SET", key, path, json);
            return ((string)result) == "OK";
        }
        
        public static string Set(this IRedisConnection connection, object obj)
        {
            var id = obj.SetId();
            var type = obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if(attr == null || attr.StorageType == StorageType.HASH)
            {
                if (obj is IRedisHydrateable hydrateable)
                {
                    connection.HSet(id, hydrateable.BuildHashSet().ToArray());
                }
                else
                {
                    connection.HSet(id, obj.BuildHashSet().ToArray());
                }
            }
            else
            {
                connection.JsonSet(id, ".", obj);
            }
            return id;
        }
        
        public static T? Get<T>(this IRedisConnection connection, string id)
            where T : notnull
        {   
            var type = typeof(T);
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if(attr == null || attr.StorageType == StorageType.HASH)
            {
                var dict = connection.HGetAll(id);
                return (T?)RedisObjectHandler.FromHashSet<T>(dict);
            }
            else
            {
                return connection.JsonGet<T>(id, ".");
            }            
        }
        
        public static async ValueTask<T?> GetAsync<T>(this IRedisConnection connection, string id)
            where T : notnull
        {   
            var type = typeof(T);
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if(attr == null || attr.StorageType == StorageType.HASH)
            {
                var dict = await connection.HGetAllAsync(id);
                return (T?)RedisObjectHandler.FromHashSet<T>(dict);
            }
            else
            {
                return connection.JsonGet<T>(id, ".");
            }            
        }
        
        public static T? JsonGet<T>(this IRedisConnection connection, string key, params string[] paths)
        {
            var args = new List<string> { key };
            args.AddRange(paths);
            var res = connection.Execute("JSON.GET", args.ToArray());
            return JsonSerializer.Deserialize<T>(((string)res));
        }
        
        public static IDictionary<string,string> HGetAll(this IRedisConnection connection, string id)
        {
            var ret = new Dictionary<string, string>();
            var res = connection.Execute("HGETALL", id).ToArray();            
            for(var i = 0; i<res.Length; i+=2)
            {
                ret.Add(res[i], res[i + 1]);
            }
            return ret;
        }
        
        public static async Task<IDictionary<string,string>> HGetAllAsync(this IRedisConnection connection, string id)
        {
            var ret = new Dictionary<string, string>();
            var res = (await connection.ExecuteAsync("HGETALL", id)).ToArray();            
            for(var i = 0; i<res.Length; i+=2)
            {
                ret.Add(res[i], res[i + 1]);
            }
            return ret;
        }
        
        public static async Task<int?> CreateAndEvalAsync(this IRedisConnection connection, string scriptName, string[] keys,
            string[] argv, string fullScript = "")
        {
            string sha;
            if (!Scripts.ShaCollection.ContainsKey(scriptName))
            {
                
                if (Scripts.ScriptCollection.ContainsKey(scriptName))
                {
                    sha = await connection.ExecuteAsync("SCRIPT","LOAD", Scripts.ScriptCollection[scriptName]);
                }
                else if (!string.IsNullOrEmpty(fullScript))
                {
                    sha = await connection.ExecuteAsync("SCRIPT","LOAD", fullScript);
                }
                else
                {
                    throw new ArgumentException($"scriptName must be amongst predefined scriptNames or a full script provided, script: {scriptName} not found");
                }

                Scripts.ShaCollection[scriptName] = sha;
            }
            var args = new List<string>();
            args.Add(Scripts.ShaCollection[scriptName]);
            args.Add(keys.Count().ToString());
            args.AddRange(keys);
            args.AddRange(argv);
            return await connection.ExecuteAsync("EVALSHA", args.ToArray());

        }
        
        public static int? CreateAndEval(this IRedisConnection connection, string scriptName, string[] keys,
            string[] argv, string fullScript = "")
        {
            string sha;
            if (!Scripts.ShaCollection.ContainsKey(scriptName))
            {
                
                if (Scripts.ScriptCollection.ContainsKey(scriptName))
                {
                    sha = connection.Execute("SCRIPT","LOAD", Scripts.ScriptCollection[scriptName]);
                }
                else if (!string.IsNullOrEmpty(fullScript))
                {
                    sha = connection.Execute("SCRIPT","LOAD", fullScript);
                }
                else
                {
                    throw new ArgumentException("scriptName must be amongst predefined scriptNames or a full script provided");
                }

                Scripts.ShaCollection[scriptName] = sha;
            }
            var args = new List<string>();
            args.Add(Scripts.ShaCollection[scriptName]);
            args.Add(keys.Count().ToString());
            args.AddRange(keys);
            args.AddRange(argv);
            return connection.Execute("EVALSHA", args.ToArray());
        }
        
        public static string Unlink(this IRedisConnection connection, string key) => connection.Execute("UNLINK", key);
    }
}