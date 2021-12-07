using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM
{
    /// <summary>
    /// Extension methods for redis commands.
    /// </summary>
    public static class RedisCommands
    {
        private static readonly JsonSerializerOptions Options = new ()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        static RedisCommands()
        {
            Options.Converters.Add(new GeoLocJsonConverter());
            Options.Converters.Add(new DateTimeJsonConverter());
        }

        /// <summary>
        /// Serializes an object to either hash or json (depending on how it's decorated), and saves it in redis.
        /// </summary>
        /// <param name="connection">connection to redis.</param>
        /// <param name="obj">the object to save.</param>
        /// <returns>the key for the object.</returns>
        public static async Task<string> SetAsync(this IRedisConnection connection, object obj)
        {
            var id = obj.SetId();
            var type = obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if (attr == null || attr.StorageType == StorageType.Hash)
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

        /// <summary>
        /// Set's values in a hash.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key.</param>
        /// <param name="fieldValues">the field value pairs to set.</param>
        /// <returns>How many new fields were created.</returns>
        public static async Task<int> HSetAsync(this IRedisConnection connection, string key, params KeyValuePair<string, string>[] fieldValues)
        {
            var args = new List<string> { key };
            foreach (var kvp in fieldValues)
            {
                args.Add(kvp.Key);
                args.Add(kvp.Value);
            }

            return await connection.ExecuteAsync("HSET", args.ToArray());
        }

        /// <summary>
        /// Sets a value as JSON in redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key for the object.</param>
        /// <param name="path">the path within the json to set.</param>
        /// <param name="json">the json.</param>
        /// <returns>whether the operation succeeded.</returns>
        public static async Task<bool> JsonSetAsync(this IRedisConnection connection, string key, string path, string json)
        {
            var result = await connection.ExecuteAsync("JSON.SET", key, path, json);
            return result == "OK";
        }

        /// <summary>
        /// Sets a value as JSON in redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key for the object.</param>
        /// <param name="path">the path within the json to set.</param>
        /// <param name="obj">the object to be converted to json.</param>
        /// <returns>whether the operation succeeded.</returns>
        public static async Task<bool> JsonSetAsync(this IRedisConnection connection, string key, string path, object obj)
        {
            var json = JsonSerializer.Serialize(obj, Options);
            var result = await connection.ExecuteAsync("JSON.SET", key, path, json);
            return result == "OK";
        }

        /// <summary>
        /// Set's values in a hash.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key.</param>
        /// <param name="fieldValues">the field value pairs to set.</param>
        /// <returns>How many new fields were created.</returns>
        public static int HSet(this IRedisConnection connection, string key, params KeyValuePair<string, string>[] fieldValues)
        {
            var args = new List<string> { key };
            foreach (var kvp in fieldValues)
            {
                args.Add(kvp.Key);
                args.Add(kvp.Value);
            }

            return (int)connection.Execute("HSET", args.ToArray());
        }

        /// <summary>
        /// Sets a value as JSON in redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key for the object.</param>
        /// <param name="path">the path within the json to set.</param>
        /// <param name="json">the json.</param>
        /// <returns>whether the operation succeeded.</returns>
        public static bool JsonSet(this IRedisConnection connection, string key, string path, string json)
        {
            var result = connection.Execute("JSON.SET", key, path, json);
            return result == "OK";
        }

        /// <summary>
        /// Sets a value as JSON in redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key for the object.</param>
        /// <param name="path">the path within the json to set.</param>
        /// <param name="obj">the object to serialize to json.</param>
        /// <returns>whether the operation succeeded.</returns>
        public static bool JsonSet(this IRedisConnection connection, string key, string path, object obj)
        {
            var json = JsonSerializer.Serialize(obj, Options);
            var result = connection.Execute("JSON.SET", key, path, json);
            return result == "OK";
        }

        /// <summary>
        /// Serializes an object to either hash or json (depending on how it's decorated), and saves it in redis.
        /// </summary>
        /// <param name="connection">connection to redis.</param>
        /// <param name="obj">the object to save.</param>
        /// <returns>the key for the object.</returns>
        public static string Set(this IRedisConnection connection, object obj)
        {
            var id = obj.SetId();
            var type = obj.GetType();
            if (Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) is not DocumentAttribute attr || attr.StorageType == StorageType.Hash)
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

        /// <summary>
        /// Gets an object of the provided type from redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="keyName">the key.</param>
        /// <typeparam name="T">The type to parse the result out to.</typeparam>
        /// <returns>the object out of redis.</returns>
        public static T? Get<T>(this IRedisConnection connection, string keyName)
            where T : notnull
        {
            var type = typeof(T);
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if (attr == null || attr.StorageType == StorageType.Hash)
            {
                var dict = connection.HGetAll(keyName);
                return (T?)RedisObjectHandler.FromHashSet<T>(dict);
            }

            return connection.JsonGet<T>(keyName, ".");
        }

        /// <summary>
        /// Gets an object of the provided type from redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="keyName">the key.</param>
        /// <typeparam name="T">The type to parse the result out to.</typeparam>
        /// <returns>the object out of redis.</returns>
        public static async ValueTask<T?> GetAsync<T>(this IRedisConnection connection, string keyName)
            where T : notnull
        {
            var type = typeof(T);
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            if (attr == null || attr.StorageType == StorageType.Hash)
            {
                var dict = await connection.HGetAllAsync(keyName);
                return (T?)RedisObjectHandler.FromHashSet<T>(dict);
            }

            return connection.JsonGet<T>(keyName, ".");
        }

        /// <summary>
        /// Get's an object out of redis.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key.</param>
        /// <param name="paths">the paths to retrieve.</param>
        /// <typeparam name="T">the type to deserialize into.</typeparam>
        /// <returns>the object pulled out of redis.</returns>
        public static T? JsonGet<T>(this IRedisConnection connection, string key, params string[] paths)
        {
            var args = new List<string> { key };
            args.AddRange(paths);
            var res = connection.Execute("JSON.GET", args.ToArray());
            return JsonSerializer.Deserialize<T>(res, Options);
        }

        /// <summary>
        /// retrieves an object out of redis and puts it into a dictionary.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="keyName">the key name.</param>
        /// <returns>the object serialized into a dictionary.</returns>
        public static IDictionary<string, string> HGetAll(this IRedisConnection connection, string keyName)
        {
            var ret = new Dictionary<string, string>();
            var res = connection.Execute("HGETALL", keyName).ToArray();
            for (var i = 0; i < res.Length; i += 2)
            {
                ret.Add(res[i], res[i + 1]);
            }

            return ret;
        }

        /// <summary>
        /// retrieves an object out of redis and puts it into a dictionary.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="keyName">the key name.</param>
        /// <returns>the object serialized into a dictionary.</returns>
        public static async Task<IDictionary<string, string>> HGetAllAsync(this IRedisConnection connection, string keyName)
        {
            var ret = new Dictionary<string, string>();
            var res = (await connection.ExecuteAsync("HGETALL", keyName)).ToArray();
            for (var i = 0; i < res.Length; i += 2)
            {
                ret.Add(res[i], res[i + 1]);
            }

            return ret;
        }

        /// <summary>
        /// Creates an evaluates a script.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="scriptName">the script's name.</param>
        /// <param name="keys">the keys to use.</param>
        /// <param name="argv">the arguments to use.</param>
        /// <param name="fullScript">the full script.</param>
        /// <returns>the result.</returns>
        /// <exception cref="ArgumentException">Thrown if the script cannot be resolved either the script is empty or the script name has not been encountered.</exception>
        public static async Task<int?> CreateAndEvalAsync(this IRedisConnection connection, string scriptName, string[] keys, string[] argv, string fullScript = "")
        {
            if (!Scripts.ShaCollection.ContainsKey(scriptName))
            {
                string sha;
                if (Scripts.ScriptCollection.ContainsKey(scriptName))
                {
                    sha = await connection.ExecuteAsync("SCRIPT", "LOAD", Scripts.ScriptCollection[scriptName]);
                }
                else if (!string.IsNullOrEmpty(fullScript))
                {
                    sha = await connection.ExecuteAsync("SCRIPT", "LOAD", fullScript);
                }
                else
                {
                    throw new ArgumentException($"scriptName must be amongst predefined scriptNames or a full script provided, script: {scriptName} not found");
                }

                Scripts.ShaCollection[scriptName] = sha;
            }

            var args = new List<string>
            {
                Scripts.ShaCollection[scriptName],
                keys.Count().ToString(),
            };
            args.AddRange(keys);
            args.AddRange(argv);
            return await connection.ExecuteAsync("EVALSHA", args.ToArray());
        }

        /// <summary>
        /// Creates an evaluates a script.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="scriptName">the script's name.</param>
        /// <param name="keys">the keys to use.</param>
        /// <param name="argv">the arguments to use.</param>
        /// <param name="fullScript">the full script.</param>
        /// <returns>the result.</returns>
        /// <exception cref="ArgumentException">Thrown if the script cannot be resolved either the script is empty or the script name has not been encountered.</exception>
        public static int? CreateAndEval(this IRedisConnection connection, string scriptName, string[] keys, string[] argv, string fullScript = "")
        {
            if (!Scripts.ShaCollection.ContainsKey(scriptName))
            {
                string sha;
                if (Scripts.ScriptCollection.ContainsKey(scriptName))
                {
                    sha = connection.Execute("SCRIPT", "LOAD", Scripts.ScriptCollection[scriptName]);
                }
                else if (!string.IsNullOrEmpty(fullScript))
                {
                    sha = connection.Execute("SCRIPT", "LOAD", fullScript);
                }
                else
                {
                    throw new ArgumentException("scriptName must be amongst predefined scriptNames or a full script provided");
                }

                Scripts.ShaCollection[scriptName] = sha;
            }

            var args = new List<string>
            {
                Scripts.ShaCollection[scriptName],
                keys.Count().ToString(),
            };
            args.AddRange(keys);
            args.AddRange(argv);
            return connection.Execute("EVALSHA", args.ToArray());
        }

        /// <summary>
        /// Unlinks a key.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="key">the key to unlink.</param>
        /// <returns>the status.</returns>
        public static string Unlink(this IRedisConnection connection, string key) => connection.Execute("UNLINK", key);
    }
}
