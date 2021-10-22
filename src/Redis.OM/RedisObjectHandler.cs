using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Redis.OM.Contracts;
using Redis.OM.Model;
using Redis.OM.Modeling;

[assembly: InternalsVisibleTo("Redis.OM.POC")]

namespace Redis.OM
{
    /// <summary>
    /// Serialize and deserialize items to and from redis data types.
    /// </summary>
    internal static class RedisObjectHandler
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new ();

        static RedisObjectHandler()
        {
            JsonSerializerOptions.Converters.Add(new GeoLocJsonConverter());
        }

        /// <summary>
        /// Builds object from provided hash set.
        /// </summary>
        /// <param name="hash">Hash set to build item from.</param>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <returns>An instance of the requested object.</returns>
        /// <exception cref="Exception">Throws an exception if Deserialization fails.</exception>
        internal static T FromHashSet<T>(IDictionary<string, string> hash)
            where T : notnull
        {
            if (typeof(IRedisHydrateable).IsAssignableFrom(typeof(T)))
            {
                var obj = Activator.CreateInstance<T>();
                ((IRedisHydrateable)obj).Hydrate(hash);
                return obj;
            }

            var attr = Attribute.GetCustomAttribute(typeof(T), typeof(DocumentAttribute)) as DocumentAttribute;
            string asJson;
            if (attr != null && attr.StorageType == StorageType.Json)
            {
                asJson = hash["$"];
            }
            else
            {
                asJson = SendToJson(hash, typeof(T));
            }

            return JsonSerializer.Deserialize<T>(asJson, JsonSerializerOptions) ?? throw new Exception("Deserialization fail");
        }

        /// <summary>
        /// Turns hash set into a basic object. To be used when you won't know the type at compile time.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <returns>the deserialized object.</returns>
        internal static object? FromHashSet(IDictionary<string, string> hash, Type type)
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
        /// Set's the id of the given field based off the objects id stratagey.
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
                if (idProperty.PropertyType == typeof(string))
                {
                    idProperty.SetValue(obj, id);
                }
                else if (idProperty.PropertyType == typeof(Guid))
                {
                    idProperty.SetValue(obj, id);
                }
                else
                {
                    throw new InvalidOperationException("Software Defined Ids on objects must either be a string or Guid");
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
            var hash = new Dictionary<string, string>();
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
        internal static IDictionary<string, string> BuildHashSet(this object obj)
        {
            if (obj is IRedisHydrateable hydrateable)
            {
                return hydrateable.BuildHashSet();
            }

            var properties = obj
                              .GetType()
                              .GetProperties()
                              .Where(x => x.GetValue(obj) != null);
            var hash = new Dictionary<string, string>();
            foreach (var property in properties)
            {
                var type = property.PropertyType;
                var propertyName = property.Name;
                ExtractPropertyName(property, ref propertyName);
                if (type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type == typeof(GeoLoc))
                {
                    var val = property.GetValue(obj);
                    if (val != null)
                    {
                        hash.Add(propertyName, val.ToString());
                    }
                }
                else if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    var e = (IEnumerable<object>)property.GetValue(obj);
                    var i = 0;
                    foreach (var v in e)
                    {
                        var innerType = v.GetType();
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

        private static string SendToJson(IDictionary<string, string> hash, Type t)
        {
            var properties = t.GetProperties();
            var ret = "{";
            foreach (var property in properties)
            {
                var type = property.PropertyType;
                var propertyName = property.Name;
                ExtractPropertyName(property, ref propertyName);
                if (!hash.Any(x => x.Key.StartsWith(propertyName)))
                {
                    continue;
                }

                if (type == typeof(bool) || type == typeof(bool?))
                {
                    ret += $"\"{propertyName}\":{hash[propertyName].ToLower()},";
                }
                else if (type.IsPrimitive || type == typeof(decimal))
                {
                    ret += $"\"{propertyName}\":{hash[propertyName]},";
                }
                else if (type == typeof(string))
                {
                    ret += $"\"{propertyName}\":\"{hash[propertyName]}\",";
                }
                else if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    var entries = hash.Where(x => x.Key.StartsWith($"{propertyName}["))
                        .ToDictionary(x => x.Key, x => x.Value);
                    var innerType = type.GetGenericArguments().SingleOrDefault();
                    if (innerType == null)
                    {
                        throw new ArgumentException("Only a single Generic type is supported on enums for the Hash type");
                    }

                    if (entries.Any())
                    {
                        ret += $"\"{propertyName}\":[";
                        for (var i = 0; i < entries.Count(); i++)
                        {
                            if (innerType == typeof(bool) || innerType == typeof(bool?))
                            {
                                var val = entries[$"{propertyName}[{i}]"];
                                ret += $"{val.ToLower()},";
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
                        .Select(x => new KeyValuePair<string, string>(x.Key.Substring($"{propertyName}.".Length), x.Value))
                        .ToDictionary(x => x.Key, x => x.Value);
                    if (entries.Any())
                    {
                        ret += $"\"{propertyName}\":";
                        ret += SendToJson(entries, type);
                        ret += ",";
                    }
                }
            }

            ret = ret.TrimEnd(',');
            ret += "}";
            return ret;
        }
    }
}
