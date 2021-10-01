using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public static class RedisIndex
    {        
        internal static DocumentAttribute? GetObjectDefinition(this Type type)
        {
            return Attribute.GetCustomAttribute(type,
                typeof(DocumentAttribute)) as DocumentAttribute;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type to be indexed</param>
        /// <exception cref="InvalidOperationException">Thrown if type provided is not decorated with a RedisObjectDefinitionAttribute</exception>
        public static string[] SerializeIndex(this Type type)
        {
            var objAttribute = Attribute.GetCustomAttribute(type, 
                typeof(DocumentAttribute)) as DocumentAttribute;
            if(objAttribute == null)
            {
                throw new InvalidOperationException($"Type being indexed must be decorated " +
                    $"with an RedisObjectDefinitionAttribute, none found on provided type:{type.Name}");
            }            
            var args = new List<string>();
            if (string.IsNullOrEmpty(objAttribute.IndexName))
            {
                args.Add($"{type.Name.ToLower()}-idx");
            }
            else
            {
                args.Add(objAttribute.IndexName);
            }
            
            args.Add("ON");
            args.Add(objAttribute.StorageType.ToString());
            args.Add("PREFIX");
            if (objAttribute.Prefixes!= null && objAttribute.Prefixes.Length > 0)
            {                
                args.Add(objAttribute.Prefixes.Length.ToString());
                args.AddRange(objAttribute.Prefixes);
            }
            else
            {
                args.Add("1");
                args.Add($"{type.FullName}:");
            }
            if (!string.IsNullOrEmpty(objAttribute.Filter))
            {
                args.Add("FILTER");
                args.Add(objAttribute.Filter);
            }
            if (!string.IsNullOrEmpty(objAttribute.Language))
            {
                args.Add("LANGUAGE");
                args.Add(objAttribute.Language);
            }
            if (!string.IsNullOrEmpty(objAttribute.LanguageField))
            {
                args.Add("LANGUAGE");
                args.Add(objAttribute.LanguageField);
            }
            args.Add("SCHEMA");
            foreach(var property in type.GetProperties())
            {
                if(objAttribute.StorageType == StorageType.HASH)
                    args.AddRange(property.SerializeArgs());
                else
                    args.AddRange(property.SerializeArgsJson());
            }

            return args.ToArray();
        }
    }
}
