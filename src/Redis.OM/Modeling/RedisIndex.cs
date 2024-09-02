using System;
using System.Collections.Generic;
using System.Linq;
using Redis.OM;
using Redis.OM.Modeling;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// A utility class for serializing objects into Redis Indices.
    /// </summary>
    public static class RedisIndex
    {
        /// <summary>
        /// Verifies whether the given index schema definition matches the current definition.
        /// </summary>
        /// <param name="redisIndexInfo">The index definition.</param>
        /// <param name="type">The type to be indexed.</param>
        /// <returns>A bool indicating whether the current index definition has drifted from the current definition, which may be used to determine when to re-create an index..</returns>
        public static bool IndexDefinitionEquals(this RedisIndexInfo redisIndexInfo, Type type)
        {
            var serialisedDefinition = SerializeIndex(type);
            var existingSet = redisIndexInfo.Attributes?.Select(a => (Property: a.Attribute!, a.Type!)).OrderBy(a => a.Property);
            var isJson = redisIndexInfo.IndexDefinition?.Identifier == "JSON";

            if (serialisedDefinition.Length < 5)
            {
                throw new ArgumentException($"Could not parse the index definition for type: {type.Name}.");
            }

            if (redisIndexInfo.IndexName != serialisedDefinition[0])
            {
                return false;
            }

            if (redisIndexInfo.IndexDefinition?.Identifier?.Equals(serialisedDefinition[2], StringComparison.OrdinalIgnoreCase) == false)
            {
                return false;
            }

            if (redisIndexInfo.IndexDefinition?.Prefixes.FirstOrDefault().Equals(serialisedDefinition[5]) == false)
            {
                return false;
            }

            var target = redisIndexInfo.Attributes?.SelectMany(a =>
            {
                var attr = new List<string>();

                if (a.Identifier == null)
                {
                    return Array.Empty<string>();
                }

                if (isJson)
                {
                    attr.Add(a.Identifier);
                    attr.Add("AS");
                }

                attr.Add(a.Attribute!);

                if (a.Type != null)
                {
                    attr.Add(a.Type);
                }

                if (a.Sortable == true)
                {
                    attr.Add("SORTABLE");
                }

                return attr.ToArray();
            });

            return target.SequenceEqual(serialisedDefinition.Skip(7));
        }

        /// <summary>
        /// Pull out the Document attribute from a Type.
        /// </summary>
        /// <param name="type">The type to pull the attribute out from.</param>
        /// <returns>A documentation attribute.</returns>
        internal static DocumentAttribute? GetObjectDefinition(this Type type)
        {
            return Attribute.GetCustomAttribute(
                type,
                typeof(DocumentAttribute)) as DocumentAttribute;
        }

        /// <summary>
        /// Serialize the Index.
        /// </summary>
        /// <param name="type">The type to be indexed.</param>
        /// <exception cref="InvalidOperationException">Thrown if type provided is not decorated with a RedisObjectDefinitionAttribute.</exception>
        /// <returns>An array of strings (the serialized args for redis).</returns>
        internal static string[] SerializeIndex(this Type type)
        {
            var objAttribute = Attribute.GetCustomAttribute(
                type,
                typeof(DocumentAttribute)) as DocumentAttribute;
            if (objAttribute == null)
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
                args.Add(objAttribute.IndexName!);
            }

            args.Add("ON");
            args.Add(objAttribute.StorageType.ToString());
            args.Add("PREFIX");
            if (objAttribute.Prefixes.Length > 0)
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
                args.Add(objAttribute.Filter!);
            }

            if (!string.IsNullOrEmpty(objAttribute.Language))
            {
                args.Add("LANGUAGE");
                args.Add(objAttribute.Language!);
            }

            if (!string.IsNullOrEmpty(objAttribute.LanguageField))
            {
                args.Add("LANGUAGE_FIELD");
                args.Add(objAttribute.LanguageField!);
            }

            if (objAttribute.Stopwords != null)
            {
                args.Add("STOPWORDS");
                args.Add(objAttribute.Stopwords.Length.ToString());
                args.AddRange(objAttribute.Stopwords);
            }

            args.Add("SCHEMA");
            foreach (var property in type.GetProperties())
            {
                args.AddRange(objAttribute.StorageType == StorageType.Hash
                    ? property.SerializeArgs()
                    : property.SerializeArgsJson());
            }

            return args.ToArray();
        }
    }
}
