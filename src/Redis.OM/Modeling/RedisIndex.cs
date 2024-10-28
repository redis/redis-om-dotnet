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
            var isJson = redisIndexInfo.IndexDefinition?.Identifier == "JSON";

            var currentOffset = 0;
            if (serialisedDefinition.Length < 5)
            {
                throw new ArgumentException($"Could not parse the index definition for type: {type.Name}.");
            }

            if (redisIndexInfo.IndexDefinition is null)
            {
                return false;
            }

            // these are properties we cannot process because FT.INFO does not respond with them
            var unprocessableProperties = new string[] { "EPSILON", "EF_RUNTIME", "PHONETIC", "STOPWORDS" };

            foreach (var property in unprocessableProperties)
            {
                if (serialisedDefinition.Any(x => x.Equals(property)))
                {
                    throw new ArgumentException($"Could not validate index definition that contains {property}");
                }
            }

            if (redisIndexInfo.IndexName != serialisedDefinition[currentOffset])
            {
                return false;
            }

            currentOffset += 2; // skip to the index type at index 2

            if (redisIndexInfo.IndexDefinition?.Identifier?.Equals(serialisedDefinition[currentOffset], StringComparison.OrdinalIgnoreCase) == false)
            {
                return false;
            }

            currentOffset += 2; // skip to prefix count

            if (!int.TryParse(serialisedDefinition[currentOffset], out var numPrefixes))
            {
                throw new ArgumentException("Could not parse index with unknown number of prefixes");
            }

            currentOffset += 2; // skip to first prefix

            if (redisIndexInfo.IndexDefinition?.Prefixes is null || redisIndexInfo.IndexDefinition.Prefixes.Length != numPrefixes || serialisedDefinition.Skip(currentOffset).Take(numPrefixes).SequenceEqual(redisIndexInfo.IndexDefinition.Prefixes))
            {
                return false;
            }

            currentOffset += numPrefixes;

            if (redisIndexInfo.IndexDefinition?.Filter is not null && !redisIndexInfo.IndexDefinition.Filter.Equals(serialisedDefinition.ElementAt(currentOffset)))
            {
                return false;
            }

            if (redisIndexInfo.IndexDefinition?.Filter is not null)
            {
                currentOffset += 2;
            }

            if (redisIndexInfo.IndexDefinition?.DefaultLanguage is not null && !redisIndexInfo.IndexDefinition.DefaultLanguage.Equals(serialisedDefinition.ElementAt(currentOffset)))
            {
                return false;
            }

            if (redisIndexInfo.IndexDefinition?.DefaultLanguage is not null)
            {
                currentOffset += 2;
            }

            if (redisIndexInfo.IndexDefinition?.LanguageField is not null && !redisIndexInfo.IndexDefinition.LanguageField.Equals(serialisedDefinition.ElementAt(currentOffset)))
            {
                return false;
            }

            if (redisIndexInfo.IndexDefinition?.LanguageField is not null)
            {
                currentOffset += 2;
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

                if (!isJson && a.Type is not null && a.Type == "VECTOR")
                {
                    attr.Add($"{a.Attribute!}.Vector");
                    attr.Add("AS");
                }

                attr.Add(a.Attribute!);

                if (a.Type != null)
                {
                    attr.Add(a.Type);
                    if (a.Type == "TAG")
                    {
                        attr.Add("SEPARATOR");
                        attr.Add(a.Separator ?? "|");
                    }

                    if (a.Type == "TEXT")
                    {
                        if (a.NoStem == true)
                        {
                            attr.Add("NOSTEM");
                        }

                        if (a.Weight is not null && a.Weight != "1")
                        {
                            attr.Add("WEIGHT");
                            attr.Add(a.Weight);
                        }
                    }

                    if (a.Type == "VECTOR")
                    {
                        if (a.Algorithm is null)
                        {
                            throw new InvalidOperationException("Encountered Vector field with no algorithm");
                        }

                        attr.Add(a.Algorithm);
                        if (a.VectorType is null)
                        {
                            throw new InvalidOperationException("Encountered vector field with no Vector Type");
                        }

                        attr.Add(NumVectorArgs(a).ToString());

                        attr.Add("TYPE");
                        attr.Add(a.VectorType);

                        if (a.Dimension is null)
                        {
                            throw new InvalidOperationException("Encountered vector field with no dimension");
                        }

                        attr.Add("DIM");
                        attr.Add(a.Dimension);

                        if (a.DistanceMetric is not null)
                        {
                            attr.Add("DISTANCE_METRIC");
                            attr.Add(a.DistanceMetric);
                        }

                        if (a.M is not null)
                        {
                            attr.Add("M");
                            attr.Add(a.M);
                        }

                        if (a.EfConstruction is not null)
                        {
                            attr.Add("EF_CONSTRUCTION");
                            attr.Add(a.EfConstruction);
                        }
                    }
                }

                if (a.Sortable == true)
                {
                    attr.Add("SORTABLE");
                }

                return attr.ToArray();
            });

            return target.SequenceEqual(serialisedDefinition.Skip(currentOffset));
        }

        /// <summary>
        /// calculates the number of arguments that would be required based to reverse engineer the index based off what
        /// is in the Info attribute.
        /// </summary>
        /// <param name="attr">The attribute.</param>
        /// <returns>The number of arguments.</returns>
        internal static int NumVectorArgs(this RedisIndexInfo.RedisIndexInfoAttribute attr)
        {
            var numArgs = 6;
            numArgs += attr.M is not null ? 2 : 0;
            numArgs += attr.EfConstruction is not null ? 2 : 0;
            return numArgs;
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
