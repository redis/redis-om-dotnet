using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Web;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;

[assembly: InternalsVisibleTo("Redis.OM.POC")]

namespace Redis.OM
{
    /// <summary>
    /// Serialize and deserialize items to and from redis data types.
    /// </summary>
    internal static class RedisObjectHandler
    {
        private static readonly Dictionary<Type, object?> TypeDefaultCache = new ()
        {
            { typeof(string), null },
            { typeof(Guid), default(Guid) },
            { typeof(Ulid), default(Ulid) },
            { typeof(int), default(int) },
            { typeof(long), default(long) },
            { typeof(uint), default(uint) },
            { typeof(ulong), default(ulong) },
        };

        /// <summary>
        /// Tries to parse the hash set into a fully or partially hydrated object.
        /// </summary>
        /// <param name="hash">The hash to generate the object from.</param>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>A fully or partially hydrated object.</returns>
        /// <exception cref="Exception">Thrown if deserialization fails.</exception>
        /// <exception cref="ArgumentException">Thrown if documentAttribute not decorating type.</exception>
        internal static T FromHashSet<T>(IDictionary<string, RedisReply> hash)
        {
            var stringDictionary = hash.ToDictionary(x => x.Key, x => x.Value.ToString());
            if (typeof(IRedisHydrateable).IsAssignableFrom(typeof(T)))
            {
                var obj = Activator.CreateInstance<T>();
                ((IRedisHydrateable)obj!).Hydrate(stringDictionary);
            }

            var attr = Attribute.GetCustomAttribute(typeof(T), typeof(DocumentAttribute)) as DocumentAttribute;
            string asJson;
            if (attr != null && attr.StorageType == StorageType.Json && hash.ContainsKey("$"))
            {
                asJson = hash["$"];
            }
            else if (hash.Keys.Count > 0 && hash.Keys.All(x => x.StartsWith("$")))
            {
                asJson = hash.Values.First();
            }
            else
            {
                asJson = SendToJson(hash, typeof(T));
            }

            var res = JsonSerializer.Deserialize<T>(asJson, RedisSerializationSettings.JsonSerializerOptions) ?? throw new Exception("Deserialization fail");
            if (hash.ContainsKey(VectorScores.NearestNeighborScoreName) || hash.Keys.Any(x => x.EndsWith(VectorScores.RangeScoreSuffix)))
            {
                var vectorScores = new VectorScores();
                if (hash.ContainsKey(VectorScores.NearestNeighborScoreName))
                {
                    vectorScores.NearestNeighborsScore = ParseScoreFromString(hash[VectorScores.NearestNeighborScoreName]);
                }

                foreach (var key in hash.Keys.Where(x => x.EndsWith(VectorScores.RangeScoreSuffix)))
                {
                    var strippedKey = key.Substring(0, key.Length - VectorScores.RangeScoreSuffix.Length);
                    var score = ParseScoreFromString(hash[key]);
                    vectorScores.RangeScores.Add(strippedKey, score);
                }

                var scoreProperties = typeof(T).GetProperties().Where(x => x.PropertyType == typeof(VectorScores));
                foreach (var p in scoreProperties)
                {
                    p.SetValue(res, vectorScores);
                }
            }

            return res;
        }

        /// <summary>
        /// Turns hash set into a basic object. To be used when you won't know the type at compile time.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <returns>the deserialized object.</returns>
        internal static object? FromHashSet(IDictionary<string, RedisReply> hash, Type type)
        {
            var asJson = SendToJson(hash, type);
            return JsonSerializer.Deserialize(asJson, type);
        }

        /// <summary>
        /// Retrieves the Id from the object.
        /// </summary>
        /// <param name="obj">Object to get id from.</param>
        /// <returns>the id, empty if no id field found.</returns>
        internal static string GetId(this object obj)
        {
            var type = obj.GetType();
            var idProperty = type.GetProperties().FirstOrDefault(x => Attribute.GetCustomAttribute(x, typeof(RedisIdFieldAttribute)) != null);
            if (idProperty != null)
            {
                return idProperty.GetValue(obj)?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the fully formed key name for the given object.
        /// </summary>
        /// <param name="obj">the object to pull the key from.</param>
        /// <returns>The key.</returns>
        /// <exception cref="ArgumentException">Thrown if type is invalid or there's no id present on the key.</exception>
        internal static string GetKey(this object obj)
        {
            var type = obj.GetType();
            var documentAttribute = (DocumentAttribute)type.GetCustomAttribute(typeof(DocumentAttribute));
            if (documentAttribute == null)
            {
                throw new ArgumentException("Missing Document Attribute on Declaring class");
            }

            var id = obj.GetId();
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id field is not correctly populated");
            }

            var sb = new StringBuilder();
            sb.Append(GetKeyPrefix(type));
            sb.Append(":");
            sb.Append(id);

            return sb.ToString();
        }

        /// <summary>
        /// Attempts to pull the key out of the object, returns false if it fails.
        /// </summary>
        /// <param name="obj">The object to pull the key out of.</param>
        /// <param name="key">The key out param.</param>
        /// <returns>True of a key was parsed, false if not.</returns>
        internal static bool TryGetKey(this object obj, out string? key)
        {
            key = null;
            var type = obj.GetType();
            var documentAttribute = type.GetCustomAttribute(typeof(DocumentAttribute)) as DocumentAttribute;

            if (documentAttribute == null)
            {
                return false;
            }

            var id = obj.GetId();
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var sb = new StringBuilder();
            sb.Append(GetKeyPrefix(type));
            sb.Append(":");
            sb.Append(id);
            key = sb.ToString();
            return true;
        }

        /// <summary>
        /// Generates the key prefix for the given type and id.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The key name.</returns>
        internal static string GetKeyPrefix(this Type type)
        {
            var documentAttribute = (DocumentAttribute)type.GetCustomAttribute(typeof(DocumentAttribute));
            if (documentAttribute == null)
            {
                throw new ArgumentException("Missing Document Attribute on Declaring class");
            }

            if (documentAttribute.Prefixes.Any())
            {
                return documentAttribute.Prefixes.First();
            }

            return type.FullName!;
        }

        /// <summary>
        /// Set's the id of the given field based off the objects id strategy.
        /// </summary>
        /// <param name="obj">The object to set the field of.</param>
        /// <returns>The id.</returns>
        /// <exception cref="InvalidOperationException">Thrown if Id property is of invalid type.</exception>
        /// <exception cref="MissingMemberException">Thrown if class is missing a document attribute decoration.</exception>
        internal static string SetId(this object obj)
        {
            var type = obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            var idProperty = type.GetProperties().FirstOrDefault(x => Attribute.GetCustomAttribute(x, typeof(RedisIdFieldAttribute)) != null);
            if (attr == null)
            {
                throw new MissingMemberException("Missing Document Attribute decoration");
            }

            var id = attr.IdGenerationStrategy.GenerateId();
            if (idProperty != null)
            {
                var idPropertyType = idProperty.PropertyType;
                var supportedIdPropertyTypes = new[] { typeof(string), typeof(Guid), typeof(Ulid) };
                if (!supportedIdPropertyTypes.Contains(idPropertyType) && !idPropertyType.IsValueType)
                {
                    throw new InvalidOperationException("Software Defined Ids on objects must either be a string, ULID, Guid, or some other value type.");
                }

                var currId = idProperty.GetValue(obj);

                if (!TypeDefaultCache.ContainsKey(idPropertyType))
                {
                    TypeDefaultCache.Add(idPropertyType, Activator.CreateInstance(idPropertyType));
                }

                if (currId?.ToString() != TypeDefaultCache[idPropertyType]?.ToString())
                {
                    id = idProperty.GetValue(obj).ToString();
                }
                else
                {
                    if (idPropertyType == typeof(Guid))
                    {
                        idProperty.SetValue(obj, Guid.Parse(id));
                    }
                    else if (idPropertyType == typeof(Ulid))
                    {
                        idProperty.SetValue(obj, Ulid.Parse(id));
                    }
                    else
                    {
                        idProperty.SetValue(obj, Convert.ChangeType(id, idProperty.PropertyType));
                    }
                }
            }

            if (attr.Prefixes == null || string.IsNullOrEmpty(attr.Prefixes.FirstOrDefault()))
            {
                return $"{type.FullName}:{id}";
            }

            return $"{attr.Prefixes.First()}:{id}";
        }

        /// <summary>
        /// Retrieve the property name.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="propertyName">The property name.</param>
        internal static void ExtractPropertyName(PropertyInfo property, ref string propertyName)
        {
            var fieldAttr = property.GetCustomAttributes(typeof(RedisFieldAttribute), true);
            if (fieldAttr.Any())
            {
                var rfa = (RedisFieldAttribute)fieldAttr.First();
                if (!string.IsNullOrEmpty(rfa.PropertyName))
                {
                    propertyName = rfa.PropertyName;
                }
            }
        }

        /// <summary>
        /// Converts redisReply to object.
        /// </summary>
        /// <param name="val">The value to initialize from.</param>
        /// <typeparam name="T">The type to initialize.</typeparam>
        /// <returns>An object initialized from the type.</returns>
        internal static T ToObject<T>(this RedisReply val)
            where T : notnull
        {
            var hash = new Dictionary<string, RedisReply>();
            var vals = val.ToArray();
            for (var i = 0; i < vals.Length; i += 2)
            {
                hash.Add(vals[i], vals[i + 1]);
            }

            return FromHashSet<T>(hash);
        }

        /// <summary>
        /// Converts object to a hash set.
        /// </summary>
        /// <param name="obj">object to be turned into a hash set.</param>
        /// <returns>A hash set generated from the object.</returns>
        internal static IDictionary<string, object> BuildHashSet(this object obj)
        {
            if (obj is IRedisHydrateable hydrateable)
            {
                return hydrateable.BuildHashSet().ToDictionary(x => x.Key, x => (object)x.Value);
            }

            var properties = obj
                              .GetType()
                              .GetProperties()
                              .Where(x => x.GetValue(obj) != null);
            var hash = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                var propertyName = property.Name;
                ExtractPropertyName(property, ref propertyName);
                if (property.GetCustomAttributes<VectorizerAttribute>().Any())
                {
                    var val = property.GetValue(obj);
                    var vectorizer = property.GetCustomAttributes<VectorizerAttribute>().First();
                    if (val is not Vector vector)
                    {
                        throw new InvalidOperationException("VectorizerAttribute must decorate vectors");
                    }

                    vector.Embed(vectorizer);
                    if (vectorizer is FloatVectorizerAttribute or DoubleVectorizerAttribute)
                    {
                        hash.Add(propertyName, vector.Embedding!);
                    }
                    else
                    {
                        hash.Add($"{propertyName}.Vector", vector.Embedding!);
                        hash.Add($"{propertyName}.Value", JsonSerializer.Serialize(vector.Obj));
                    }

                    continue;
                }

                if (type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type == typeof(GeoLoc) || type == typeof(Ulid) || type == typeof(Guid))
                {
                    var val = property.GetValue(obj);
                    if (val != null)
                    {
                        hash.Add(propertyName, Convert.ToString(val, CultureInfo.InvariantCulture));
                    }
                }
                else if (type.IsEnum)
                {
                    var val = property.GetValue(obj);
                    hash.Add(propertyName, ((int)val).ToString());
                }
                else if (type == typeof(DateTimeOffset))
                {
                    var val = (DateTimeOffset)property.GetValue(obj);
                    if (val != null)
                    {
                        hash.Add(propertyName, val.ToString("O"));
                    }
                }
                else if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    var val = (DateTime)property.GetValue(obj);
                    if (val != default)
                    {
                        hash.Add(propertyName, new DateTimeOffset(val).ToUnixTimeMilliseconds().ToString());
                    }
                }
                else if (type == typeof(Vector))
                {
                    var val = (Vector)obj;
                    if (val.Embedding is null)
                    {
                        throw new InvalidOperationException("Could not use null embedding.");
                    }

                    hash.Add(propertyName, val.Embedding);
                }
                else if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    IEnumerable<object> e;
                    var innerType = GetEnumerableType(property);

                    if (innerType.IsPrimitive || innerType == typeof(decimal))
                    {
                        e = PrimitiveCollectionToStrings(property, obj, innerType);
                    }
                    else
                    {
                        e = (IEnumerable<object>)property.GetValue(obj);
                    }

                    var i = 0;
                    foreach (var v in e)
                    {
                        if (innerType.IsPrimitive || innerType == typeof(decimal) || innerType == typeof(string))
                        {
                            hash.Add($"{propertyName}[{i}]", v.ToString());
                        }
                        else
                        {
                            var subHash = v.BuildHashSet();
                            foreach (var kvp in subHash)
                            {
                                hash.Add($"{propertyName}.[{i}].{kvp.Key}", kvp.Value);
                            }
                        }

                        i++;
                    }
                }
                else
                {
                    var subHash = property.GetValue(obj)?.BuildHashSet();
                    if (subHash != null)
                    {
                        foreach (var kvp in subHash)
                        {
                            hash.Add($"{propertyName}.{kvp.Key}", kvp.Value);
                        }
                    }
                }
            }

            return hash;
        }

        private static string SendToJson(IDictionary<string, RedisReply> hash, Type t)
        {
            var properties = t.GetProperties();
            if ((!properties.Any() || t == typeof(Ulid) || t == typeof(Ulid?)) && hash.Count == 1)
            {
                return $"\"{hash.First().Value}\"";
            }

            var ret = "{";
            foreach (var property in properties)
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var propertyName = property.Name;
                ExtractPropertyName(property, ref propertyName);
                var vectorizer = property.GetCustomAttributes<VectorizerAttribute>().FirstOrDefault();
                if (vectorizer is FloatVectorizerAttribute || vectorizer is DoubleVectorizerAttribute)
                {
                    if (hash.ContainsKey(propertyName))
                    {
                        string arrString;
                        if (vectorizer.VectorType == VectorType.FLOAT32)
                        {
                            var floats = VectorUtils.VectorStrToFloats(hash[propertyName]);
                            arrString = string.Join(",", floats);
                        }
                        else
                        {
                            var doubles = VectorUtils.VecStrToDoubles(hash[propertyName]);
                            arrString = string.Join(",", doubles);
                        }

                        var valueStr = $"[{arrString}]";
                        ret += $"\"{propertyName}\":{valueStr},";
                    }

                    continue;
                }

                var isVectorized = vectorizer != null;
                var lookupPropertyName = propertyName + (isVectorized ? ".Value" : string.Empty);
                var vectorPropertyName = $"{propertyName}.Vector";
                if (isVectorized && !hash.ContainsKey($"{propertyName}.Value") && !hash.ContainsKey($"{propertyName}.Vector"))
                {
                    continue;
                }

                if (isVectorized)
                {
                    ret += $"\"{propertyName}\":{{";
                    propertyName = "Value";
                }

                if (!hash.Any(x => x.Key.StartsWith(lookupPropertyName)))
                {
                    continue;
                }

                if (type == typeof(bool) || type == typeof(bool?))
                {
                    if (!hash.ContainsKey(lookupPropertyName))
                    {
                        continue;
                    }

                    ret += $"\"{propertyName}\":{((string)hash[lookupPropertyName]).ToLower()},";
                }
                else if (type.IsPrimitive || type == typeof(decimal) || type.IsEnum)
                {
                    if (!hash.ContainsKey(lookupPropertyName))
                    {
                        continue;
                    }

                    ret += $"\"{propertyName}\":{hash[lookupPropertyName]},";
                }
                else if (type == typeof(string) || type == typeof(GeoLoc) || type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(DateTimeOffset) || type == typeof(Guid) || type == typeof(Guid?) || type == typeof(Ulid) || type == typeof(Ulid?))
                {
                    if (!hash.ContainsKey(lookupPropertyName))
                    {
                        continue;
                    }

                    ret += $"\"{propertyName}\":\"{HttpUtility.JavaScriptStringEncode(hash[lookupPropertyName])}\",";
                }
                else if (type == typeof(Vector))
                {
                    if (!hash.ContainsKey(lookupPropertyName))
                    {
                        continue;
                    }

                    string arrString;
                    if (type == typeof(float[]))
                    {
                        var floats = VectorUtils.VectorStrToFloats(hash[lookupPropertyName]);
                        arrString = string.Join(",", floats);
                    }
                    else
                    {
                        var doubles = VectorUtils.VecStrToDoubles(hash[lookupPropertyName]);
                        arrString = string.Join(",", doubles);
                    }

                    var valueStr = $"[{arrString}]";
                    ret += $"\"{lookupPropertyName}\":{valueStr},";
                }
                else if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    var entries = hash.Where(x => x.Key.StartsWith($"{propertyName}["))
                        .ToDictionary(x => x.Key, x => x.Value);
                    var innerType = GetEnumerableType(property);
                    if (innerType == null)
                    {
                        throw new ArgumentException("Only a single Generic type is supported on enums for the Hash type");
                    }

                    if (innerType == typeof(byte) && entries.Any())
                    {
                        var bytes = entries.Select(x => byte.Parse(x.Value)).ToArray();
                        ret += $"\"{propertyName}\":\"";
                        ret += Convert.ToBase64String(bytes);
                        ret += "\",";
                    }
                    else if (entries.Any())
                    {
                        ret += $"\"{propertyName}\":[";
                        for (var i = 0; i < entries.Count(); i++)
                        {
                            if (innerType == typeof(bool) || innerType == typeof(bool?))
                            {
                                var val = entries[$"{propertyName}[{i}]"];
                                ret += $"{((string)val).ToLower()},";
                            }
                            else if (innerType.IsPrimitive || innerType == typeof(decimal))
                            {
                                var val = entries[$"{propertyName}[{i}]"];
                                ret += $"{val},";
                            }
                            else if (innerType == typeof(string))
                            {
                                var val = entries[$"{propertyName}[{i}]"];
                                ret += $"\"{val}\",";
                            }
                            else
                            {
                                var dictionary = entries.Where(x => x.Key.StartsWith($"{propertyName}[{i}]"))
                                    .Select(x => new KeyValuePair<string, string>(
                                        x.Key.Substring($"{propertyName}[{i}]".Length), x.Value))
                                    .ToDictionary(x => x.Key, x => x.Value);
                                if (dictionary.Any())
                                {
                                    ret += SendToJson(entries, innerType);
                                    ret += ",";
                                }
                            }
                        }

                        ret = ret.TrimEnd(',');
                        ret += "],";
                    }
                }
                else
                {
                    var entries = hash.Where(x => x.Key.StartsWith($"{propertyName}."))
                        .Select(x => new KeyValuePair<string, RedisReply>(x.Key.Substring($"{propertyName}.".Length), x.Value))
                        .ToDictionary(x => x.Key, x => x.Value);
                    if (entries.Any())
                    {
                        ret += $"\"{propertyName}\":";
                        ret += SendToJson(entries, type);
                        ret += ",";
                    }
                    else if (hash.ContainsKey(propertyName))
                    {
                        ret += $"\"{propertyName}\":";
                        ret += hash[lookupPropertyName];
                        ret += ",";
                    }
                }

                if (isVectorized)
                {
                    if (vectorizer is null)
                    {
                        throw new InvalidOperationException(
                            "Vector field must be decorated with a vectorizer attribute");
                    }

                    if (hash.ContainsKey(lookupPropertyName))
                    {
                        ret += $"\"Value\":\"{HttpUtility.JavaScriptStringEncode(hash[lookupPropertyName])}\",";
                    }

                    string arrString;
                    if (vectorizer.VectorType == VectorType.FLOAT32)
                    {
                        var floats = VectorUtils.VectorStrToFloats(hash[vectorPropertyName]);
                        arrString = string.Join(",", floats);
                    }
                    else
                    {
                        var doubles = VectorUtils.VecStrToDoubles(hash[vectorPropertyName]);
                        arrString = string.Join(",", doubles);
                    }

                    var valueStr = $"[{arrString}]";
                    ret += $"\"Vector\":{valueStr}}},";
                }
            }

            ret = ret.TrimEnd(',');
            ret += "}";
            return ret;
        }

        private static double ParseScoreFromString(string scoreStr)
        {
            if (double.TryParse(scoreStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var score))
            {
                return score;
            }

            return scoreStr switch
            {
                "inf" => double.PositiveInfinity,
                "-inf" => double.NegativeInfinity,
                "nan" => double.NaN,
                _ => throw new ArgumentException($"Could not parse score from {scoreStr}", nameof(scoreStr))
            };
        }

        private static Type GetEnumerableType(PropertyInfo pi)
        {
            var type = pi.PropertyType.GetElementType();
            if (type == null && pi.PropertyType.GetGenericArguments().Any())
            {
                type = pi.PropertyType.GetGenericArguments()[0];
            }

            if (type == null)
            {
                throw new ArgumentException("Could not pull generic type out of collection");
            }

            type = Nullable.GetUnderlyingType(type) ?? type;
            return type;
        }

        private static byte[] PrimitiveCollectionToVectorBytes(PropertyInfo pi, object obj, Type type)
        {
            if (type == typeof(double))
            {
                return ((IEnumerable<double>)pi.GetValue(obj)).SelectMany(BitConverter.GetBytes).ToArray();
            }

            if (type == typeof(float))
            {
                return ((IEnumerable<float>)pi.GetValue(obj)).SelectMany(BitConverter.GetBytes).ToArray();
            }

            throw new ArgumentException("Could not pull a usable type out from property info");
        }

        private static IEnumerable<string> PrimitiveCollectionToStrings(PropertyInfo pi, object obj, Type type)
        {
            if (type == typeof(bool))
            {
                return ((IEnumerable<bool>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(byte))
            {
                return ((IEnumerable<byte>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(sbyte))
            {
                return ((IEnumerable<sbyte>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(short))
            {
                return ((IEnumerable<short>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(ushort))
            {
                return ((IEnumerable<ushort>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(int))
            {
                return ((IEnumerable<int>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(uint))
            {
                return ((IEnumerable<uint>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(long))
            {
                return ((IEnumerable<ulong>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(ulong))
            {
                return ((IEnumerable<ulong>)pi.GetValue(obj)).Select(x => x.ToString());
            }

            if (type == typeof(char))
            {
                return ((IEnumerable<char>)pi.GetValue(obj)).Select(x => $"\"{x.ToString()}\"");
            }

            if (type == typeof(double))
            {
                return ((IEnumerable<double>)pi.GetValue(obj)).Select(x => x.ToString(CultureInfo.InvariantCulture));
            }

            if (type == typeof(float))
            {
                return ((IEnumerable<float>)pi.GetValue(obj)).Select(x => x.ToString(CultureInfo.InvariantCulture));
            }

            if (type == typeof(decimal))
            {
                return ((IEnumerable<decimal>)pi.GetValue(obj)).Select(x => x.ToString(CultureInfo.InvariantCulture));
            }

            throw new ArgumentException("Could not pull a usable type out from property info");
        }
    }
}
