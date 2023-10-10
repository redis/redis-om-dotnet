using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Manages the state of the Redis Collection.
    /// </summary>
    public class RedisCollectionStateManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollectionStateManager"/> class.
        /// </summary>
        /// <param name="attr">The document attribute for the type.</param>
        public RedisCollectionStateManager(DocumentAttribute attr)
        {
            DocumentAttribute = attr;
        }

        /// <summary>
        /// Gets the DocumentAttribute for the underlying type.
        /// </summary>
        public DocumentAttribute DocumentAttribute { get; }

        /// <summary>
        /// Gets a snapshot from when the collection enumerated.
        /// </summary>
        internal IDictionary<string, object> Snapshot { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the data in its current state.
        /// </summary>
        internal IDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// Clears out all the data in the state manager at re-enumeration.
        /// </summary>
        internal void Clear()
        {
            Snapshot.Clear();
            Data.Clear();
        }

        /// <summary>
        /// Removes the key from the data and snapshot.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        internal void Remove(string key)
        {
            Snapshot.Remove(key);
            Data.Remove(key);
        }

        /// <summary>
        /// Add item to data.
        /// </summary>
        /// <param name="key">the item's key.</param>
        /// <param name="value">the item's value.</param>
        internal void InsertIntoData(string key, object value)
        {
            Data.Remove(key);
            Data.Add(key, value);
        }

        /// <summary>
        /// Add item to snapshot.
        /// </summary>
        /// <param name="key">the item's key.</param>
        /// <param name="value">the current value of the item.</param>
        internal void InsertIntoSnapshot(string key, object value)
        {
            Snapshot.Remove(key);

            if (DocumentAttribute.StorageType == StorageType.Json)
            {
                var json = JToken.FromObject(value, Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                Snapshot.Add(key, json);
            }
            else
            {
                var hash = value.BuildHashSet();
                Snapshot.Add(key, hash);
            }
        }

        /// <summary>
        /// Builds a diff for a single object from what's currently in the snapshot.
        /// </summary>
        /// <param name="key">the key of the object in redis.</param>
        /// <param name="value">The current value.</param>
        /// <param name="differences">The detected differences.</param>
        /// <returns>Whether a diff could be constructed.</returns>
        internal bool TryDetectDifferencesSingle(string key, object value, out IList<IObjectDiff>? differences)
        {
            if (!Snapshot.ContainsKey(key))
            {
                differences = null;
                return false;
            }

            if (DocumentAttribute.StorageType == StorageType.Json)
            {
                var dataJson = JsonSerializer.Serialize(value, RedisSerializationSettings.JsonSerializerOptions);
                var current = JsonConvert.DeserializeObject<JObject>(dataJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DateFormatHandling = DateFormatHandling.IsoDateFormat, DateParseHandling = DateParseHandling.DateTimeOffset, DateTimeZoneHandling = DateTimeZoneHandling.Utc });
                var snapshot = (JToken)Snapshot[key];
                var diff = FindDiff(current!, snapshot);
                differences = BuildJsonDifference(diff, "$", snapshot);
            }
            else
            {
                var dataHash = value.BuildHashSet();
                var snapshotHash = (IDictionary<string, object>)Snapshot[key];
                var deletedKeys = snapshotHash.Keys.Except(dataHash.Keys).Select(x => new KeyValuePair<string, string>(x, string.Empty));
                var modifiedKeys = dataHash.Where(x =>
                    !snapshotHash.Keys.Contains(x.Key) || snapshotHash[x.Key] != x.Value).Select(x =>
                    new KeyValuePair<string, string>(x.Key, x.Value.ToString()));
                differences = new List<IObjectDiff>
                {
                    new HashDiff(modifiedKeys, deletedKeys.Select(x => x.Key)),
                };
            }

            return true;
        }

        /// <summary>
        /// Detects the differences.
        /// </summary>
        /// <returns>a difference dictionary.</returns>
        internal IDictionary<string, IList<IObjectDiff>> DetectDifferences()
        {
            var res = new Dictionary<string, IList<IObjectDiff>>();
            if (DocumentAttribute.StorageType == StorageType.Json)
            {
                foreach (var key in Snapshot.Keys.ToArray())
                {
                    if (Data.ContainsKey(key))
                    {
                        var dataJson = JsonSerializer.Serialize(Data[key], RedisSerializationSettings.JsonSerializerOptions);
                        var current = JsonConvert.DeserializeObject<JObject>(dataJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        var snapshot = (JToken)Snapshot[key];
                        var diff = FindDiff(current!, snapshot);
                        var diffArgs = BuildJsonDifference(diff, "$", snapshot);
                        res.Add(key, diffArgs);
                    }
                    else
                    {
                        res.Add(key, new List<IObjectDiff> { new JsonDiff("DEL", ".", string.Empty) });
                    }
                }
            }
            else
            {
                foreach (var key in Snapshot.Keys)
                {
                    if (Data.ContainsKey(key))
                    {
                        var dataHash = Data[key] !.BuildHashSet();
                        var snapshotHash = (IDictionary<string, object>)Snapshot[key];
                        var deletedKeys = snapshotHash.Keys.Except(dataHash.Keys).Select(x => new KeyValuePair<string, string>(x, string.Empty));
                        var modifiedKeys = dataHash.Where(x =>
                            !snapshotHash.Keys.Contains(x.Key) || snapshotHash[x.Key] != x.Value).Select(x =>
                            new KeyValuePair<string, string>(x.Key, x.Value.ToString()));
                        var diff = new List<IObjectDiff>
                        {
                            new HashDiff(modifiedKeys, deletedKeys.Select(x => x.Key)),
                        };
                        res.Add(key, diff);
                    }
                    else
                    {
                        res.Add(key, new List<IObjectDiff> { new DelDiff() });
                    }
                }
            }

            return res;
        }

        private static IList<IObjectDiff> BuildJsonDifference(JObject diff, string currentPath, JToken snapshot)
        {
            var ret = new List<IObjectDiff>();
            if (diff.ContainsKey("+") && diff.ContainsKey("-"))
            {
                if (diff["+"] is JArray arr)
                {
                    var minusArr = (JArray)diff["-"] !;
                    ret.AddRange(arr.Select(item => new JsonDiff("ARRAPPEND", currentPath, item)));

                    ret.AddRange(minusArr.Select(item => new JsonDiff("ARRREM", currentPath, item)));
                }
                else
                {
                    ret.Add(new JsonDiff("SET", currentPath, diff["+"] !));
                }

                return ret;
            }

            if (diff.ContainsKey("+"))
            {
                if (diff["+"] is JArray arr && snapshot.SelectToken(diff.Path) is not null)
                {
                    ret.AddRange(arr.Select(item => new JsonDiff("ARRAPPEND", currentPath, item)));
                }
                else
                {
                    ret.Add(new JsonDiff("SET", currentPath, diff["+"] !));
                }

                return ret;
            }

            if (diff.ContainsKey("-"))
            {
                if (diff["-"] is JArray arr)
                {
                    ret.AddRange(arr.Select(item => new JsonDiff("ARRREM", currentPath, item)));
                }
                else
                {
                    ret.Add(new JsonDiff("DEL", currentPath, string.Empty));
                }

                return ret;
            }

            foreach (var item in diff)
            {
                var val = item.Value as JObject;
                ret.AddRange(BuildJsonDifference(val!, $"{currentPath}.{item.Key}", snapshot));
            }

            return ret;
        }

        /// <summary>
        /// Builds a difference JObject between json objects
        /// lifted with some minor modifications from Dmitry Polyakov's answer in SO
        /// https://stackoverflow.com/a/53654737/7299345.
        /// </summary>
        /// <param name="currentObject">current object.</param>
        /// <param name="snapshotObject">snapshot object.</param>
        /// <returns>a jobject containing a diff.</returns>
        private static JObject FindDiff(JToken currentObject, JToken snapshotObject)
        {
            var diff = new JObject();
            if (JToken.DeepEquals(currentObject, snapshotObject))
            {
                return diff;
            }

            switch (currentObject.Type)
            {
                case JTokenType.Object:
                    {
                        var current = currentObject as JObject;
                        var model = snapshotObject as JObject;
                        if (current == null && model != null)
                        {
                            return new JObject { ["-"] = model };
                        }

                        if (current != null && model == null)
                        {
                            return new JObject { ["+"] = current };
                        }

                        var addedKeys = current!.Properties()
                            .Select(c => c.Name).Except(model!.Properties().Select(c => c.Name));
                        var removedKeys = model.Properties()
                            .Select(c => c.Name).Except(current.Properties().Select(c => c.Name));
                        var unchangedKeys = current.Properties()
                            .Where(c => JToken.DeepEquals(c.Value, snapshotObject[c.Name])).Select(c => c.Name);
                        var enumerable = addedKeys as string[] ?? addedKeys.ToArray();
                        foreach (var k in enumerable)
                        {
                            diff[k] = new JObject
                            {
                                ["+"] = currentObject[k],
                            };
                        }

                        foreach (var k in removedKeys)
                        {
                            diff[k] = new JObject
                            {
                                ["-"] = snapshotObject[k],
                            };
                        }

                        var potentiallyModifiedKeys = current.Properties().Select(c => c.Name).Except(enumerable).Except(unchangedKeys);
                        foreach (var k in potentiallyModifiedKeys)
                        {
                            var foundDiff = FindDiff(current[k] !, model[k] !);
                            if (foundDiff.HasValues)
                            {
                                diff[k] = foundDiff;
                            }
                        }
                    }

                    break;
                case JTokenType.Array:
                    {
                        var current = currentObject as JArray;
                        var model = snapshotObject as JArray;
                        var plus = new JArray(current!.Except(model!, new JTokenEqualityComparer()));
                        var minus = new JArray(model!.Except(current!, new JTokenEqualityComparer()));
                        if (plus.HasValues)
                        {
                            diff["+"] = plus;
                        }

                        if (minus.HasValues)
                        {
                            diff["-"] = minus;
                        }
                    }

                    break;
                default:
                    if (currentObject.ToString() != snapshotObject.ToString())
                    {
                        diff["+"] = currentObject;
                        diff["-"] = snapshotObject;
                    }

                    break;
            }

            return diff;
        }
    }
}
