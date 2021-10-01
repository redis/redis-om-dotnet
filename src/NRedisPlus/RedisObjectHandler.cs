using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NRedisPlus.RediSearch;
using System.Text.Json;

namespace NRedisPlus
{
    public static class RedisObjectHandler
    {
        
        public static T FromHashSet<T>(IDictionary<string, string> hash)
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
            if(attr != null && attr.StorageType == StorageType.JSON)
            {
                asJson = hash["$"];
            }
            else
            {
                asJson = SendToJson(hash, typeof(T));
            }
            
            return JsonSerializer.Deserialize<T>(asJson) ?? throw new Exception("Deserialization fail");
        }

        public static object? FromHashSet(IDictionary<string,string> hash, Type type)
        {
            var asJson = SendToJson(hash, type);
            return JsonSerializer.Deserialize(asJson, type);
        }

        public static string GetId(this object obj)
        {
            var type = obj.GetType();
            var idProperty = type.GetProperties().Where(x => Attribute.GetCustomAttribute(x, typeof(RedisIdFieldAttribute)) != null).FirstOrDefault();
            if(idProperty != null) 
            {
                return idProperty.GetValue(obj)?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        public static string SetId(this object obj)
        {
            var type = obj.GetType();
            var attr = Attribute.GetCustomAttribute(type, typeof(DocumentAttribute)) as DocumentAttribute;
            var properties = type.GetProperties();
            var idProperty = type.GetProperties().Where(x => Attribute.GetCustomAttribute(x, typeof(RedisIdFieldAttribute)) != null).FirstOrDefault();
            var id = attr.IdGenerationStrategy.GenerateId();
            if (idProperty != null)
            {
                if(idProperty.PropertyType == typeof(string) )
                {
                    idProperty.SetValue(obj, id.ToString());
                }
                else if(idProperty.PropertyType == typeof(Guid))
                {
                    idProperty.SetValue(obj, id);
                }
                else
                {
                    throw new InvalidOperationException("Software Defined Ids on objects must either be a string or Guid");
                }                    
            }
            if(attr == null || attr.Prefixes == null || string.IsNullOrEmpty(attr.Prefixes.FirstOrDefault()))
            {
                return $"{type.FullName}:{id}";
            }
            return $"{attr.Prefixes.First()}:{id}";
        }
        public static void ExtractPropertyName(PropertyInfo property, ref string propertyName)
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

        public static T ToObject<T>(this RedisReply val)
            where T : notnull
        {
            var hash = new Dictionary<string, string>();
            var vals = val.ToArray();
            for(var i = 0; i < vals.Length; i+=2)
            {
                hash.Add(vals[i], vals[i + 1]);
            }
            return (T)FromHashSet<T>(hash);
        }

        public static IDictionary<string,string> BuildHashSet(this object obj)
        {
            if(obj is IRedisHydrateable hydrateable)
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
                if(type.IsPrimitive || type == typeof(decimal) || type == typeof(string))
                {
                    var val = property.GetValue(obj);
                    if(val!=null)
                        hash.Add(propertyName, val.ToString());
                }
                else if (type.GetInterfaces().Any(x=>x.IsGenericType && x.GetGenericTypeDefinition()==typeof(IEnumerable<>)))
                {
                    var e = (IEnumerable<object>)property.GetValue(obj);
                    var i = 0;
                    foreach(var v in e)
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
                    continue;
                if(type == typeof(bool) || type == typeof(bool?))
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
                        throw new ArgumentException("Only a single Generic type is supported on enums for the Hash type");

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
                                entries.Where(x => x.Key.StartsWith($"{propertyName}[{i}]"))
                                    .Select(x => new KeyValuePair<string, string>(
                                        x.Key.Substring($"{propertyName}[{i}]".Length), x.Value))
                                    .ToDictionary(x => x.Key, x => x.Value);
                                if (entries.Any())
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
                        .Select(x=>new KeyValuePair<string,string>(x.Key.Substring($"{propertyName}.".Length),x.Value))
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
