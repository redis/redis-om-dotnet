using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redis.OM;
using Redis.OM.Modeling;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Manages the state of the Redis Collection.
    /// </summary>
    public class RedisCollectionStateManager
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly Func<object, object> _snapshotValueFn;

        private readonly Func<bool, object, object, IList<IObjectDiff>> _detectDifferenceFn;

        static RedisCollectionStateManager()
        {
            JsonSerializerOptions.Converters.Add(new GeoLocJsonConverter());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollectionStateManager"/> class.
        /// </summary>
        /// <param name="attr">The document attribute for the type.</param>
        public RedisCollectionStateManager(DocumentAttribute attr)
        {
            if (attr.StorageType == StorageType.Json)
            {
                this._detectDifferenceFn = DetectDifferenceJson;
                this._snapshotValueFn = SnapshotValueJson;
            }
            else
            {
                this._detectDifferenceFn = DetectDifferenceHash;
                this._snapshotValueFn = SnapshotValueHash;
            }
        }

        /// <summary>
        /// Gets a snapshot from when the collection enumerated.
        /// </summary>
        internal IDictionary<string, object> Snapshot { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the data in its current state.
        /// </summary>
        internal IDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// Add item to snapshot.
        /// </summary>
        /// <param name="key">the item's key.</param>
        /// <param name="value">the current value of the item.</param>
        internal void InsertIntoSnapshot(string key, object value)
        {
            if (Snapshot.ContainsKey(key))
            {
                return;
            }

            var snapshotPiece = this._snapshotValueFn(value);
            Snapshot.Add(key, snapshotPiece);
        }

        /// <summary>
        /// Detects the differences.
        /// </summary>
        /// <returns>a difference dictionary.</returns>
        internal IDictionary<string, IList<IObjectDiff>> DetectDifferences()
        {
            var res = new Dictionary<string, IList<IObjectDiff>>();
            foreach (var key in Snapshot.Keys)
            {
                var found = Data.ContainsKey(key);
                var value = Data[key] !;
                var snapshot = Snapshot[key];
                var diff = this._detectDifferenceFn(found, value, snapshot);
                res.Add(key, diff);
            }

            return res;
        }

        private static IList<IObjectDiff> BuildJsonDifference(JObject diff, string currentPath)
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
                if (diff["+"] is JArray arr)
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
                ret.AddRange(BuildJsonDifference(val!, $"{currentPath}.{item.Key}"));
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
                    diff["+"] = currentObject;
                    diff["-"] = snapshotObject;
                    break;
            }

            return diff;
        }

        private static object SnapshotValueJson(object value)
        {
            var json = JToken.FromObject(value, Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            return json;
        }

        private static object SnapshotValueHash(object value)
        {
            var hash = value.BuildHashSet();
            return hash;
        }

        private static IList<IObjectDiff> DetectDifferenceJson(bool found, object value, object snapshotObject)
        {
            if (found)
            {
                var dataJson = JsonSerializer.Serialize(value, JsonSerializerOptions);
                var current = JsonConvert.DeserializeObject<JObject>(dataJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var snapshot = (JToken)snapshotObject;
                var diff = FindDiff(current!, snapshot);
                var diffArgs = BuildJsonDifference(diff, "$");
                return diffArgs;
            }
            else
            {
                return new List<IObjectDiff> { new JsonDiff("DEL", ".", string.Empty) };
            }
        }

        private static IList<IObjectDiff> DetectDifferenceHash(bool found, object value, object snapshotObject)
        {
            if (found)
            {
                var dataHash = value!.BuildHashSet();
                var snapshotHash = (IDictionary<string, string>)snapshotObject;
                var deletedKeys = snapshotHash.Keys.Except(dataHash.Keys).Select(x => new KeyValuePair<string, string>(x, string.Empty));
                var modifiedKeys = dataHash.Where(x =>
                    !snapshotHash.Keys.Contains(x.Key) || snapshotHash[x.Key] != x.Value);
                var diff = new List<IObjectDiff>
                {
                    new HashDiff(modifiedKeys, deletedKeys.Select(x => x.Key)),
                };

                return diff;
            }
            else
            {
                return new List<IObjectDiff> { new DelDiff() };
            }
        }
    }
}
