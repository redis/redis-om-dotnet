using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json.Serialization;
using Redis.OM.Modeling.Vectors;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// utility methods for serializing schema fields.
    /// </summary>
    internal static class RedisSchemaField
    {
        /// <summary>
        /// Checks the type to see if it's a complex type that cannot be used as a scalar in RediSearch.
        /// </summary>
        /// <param name="type">The Type to check.</param>
        /// <returns>Whether not we consider the type to be complex.</returns>
        internal static bool IsComplexType(Type type)
        {
            return !TypeDeterminationUtilities.IsNumeric(type)
                && type != typeof(string)
                && type != typeof(GeoLoc)
                && type != typeof(Ulid)
                && type != typeof(bool)
                && type != typeof(Guid)
                && !type.IsEnum
                && !IsTypeIndexableArray(type);
        }

        /// <summary>
        /// gets the schema field args serialized for json.
        /// </summary>
        /// <param name="info">The property info.</param>
        /// <param name="remainingDepth">The remaining allowable depth in the reccurance.</param>
        /// <param name="pathPrefix">The current prefix of the parent attribute.</param>
        /// <param name="aliasPrefix">The prefix of the alias.</param>
        /// <returns>The create index args for the schema field for JSON.</returns>
        internal static string[] SerializeArgsJson(this PropertyInfo info, int remainingDepth = -1, string pathPrefix = "$.", string aliasPrefix = "")
        {
            var attributes = info.GetCustomAttributes()
                .Where(x => x is SearchFieldAttribute)
                .Cast<SearchFieldAttribute>()
                .ToArray();

            if (!attributes.Any())
            {
                return Array.Empty<string>();
            }

            var ret = new List<string>();
            foreach (var attr in attributes)
            {
                int cascadeDepth = remainingDepth == -1 ? attr.CascadeDepth : remainingDepth;
                if (attr.JsonPath != null)
                {
                    ret.AddRange(SerializeIndexFromJsonPaths(info, attr, pathPrefix, aliasPrefix, cascadeDepth));
                }
                else
                {
                    var innerType = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;

                    if (attr is IndexedAttribute indexedAttribute && (innerType == typeof(Vector) || innerType.BaseType == typeof(Vector)))
                    {
                        var vectorizer = info.GetCustomAttributes<VectorizerAttribute>().FirstOrDefault(x => x.GetType() != typeof(FloatVectorizerAttribute) && x.GetType() != typeof(DoubleVectorizerAttribute));
                        var pathPostfix = vectorizer != null ? ".Vector" : string.Empty;
                        ret.Add(!string.IsNullOrEmpty(attr.PropertyName) ? $"{pathPrefix}{attr.PropertyName}{pathPostfix}" : $"{pathPrefix}{info.Name}{pathPostfix}");
                        ret.Add("AS");
                        ret.Add(!string.IsNullOrEmpty(attr.PropertyName) ? $"{aliasPrefix}{attr.PropertyName}" : $"{aliasPrefix}{info.Name}");
                        ret.AddRange(CommonSerialization(attr, innerType, info));
                    }
                    else if (IsComplexType(innerType))
                    {
                        if (cascadeDepth > 0)
                        {
                            foreach (var property in info.PropertyType.GetProperties())
                            {
                                ret.AddRange(property.SerializeArgsJson(cascadeDepth - 1, $"{pathPrefix}{info.Name}.", string.IsNullOrEmpty(aliasPrefix) ? $"{info.Name}_" : $"{aliasPrefix}{info.Name}_"));
                            }
                        }
                    }
                    else
                    {
                        var pathPostFix = IsTypeIndexableArray(innerType) ? "[*]" : string.Empty;
                        ret.Add(!string.IsNullOrEmpty(attr.PropertyName) ? $"{pathPrefix}{attr.PropertyName}{pathPostFix}" : $"{pathPrefix}{info.Name}{pathPostFix}");
                        ret.Add("AS");
                        ret.Add(!string.IsNullOrEmpty(attr.PropertyName) ? $"{aliasPrefix}{attr.PropertyName}" : $"{aliasPrefix}{info.Name}");
                        ret.AddRange(CommonSerialization(attr, innerType, info));
                    }
                }
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Serializes the property info into index arguments.
        /// </summary>
        /// <param name="info">the property info.</param>
        /// <returns>FT.CREATE serialized args.</returns>
        internal static string[] SerializeArgs(this PropertyInfo info)
        {
            var attr = Attribute.GetCustomAttribute(info, typeof(SearchFieldAttribute)) as SearchFieldAttribute;
            if (attr is null)
            {
                return Array.Empty<string>();
            }

            var suffix = string.Empty;
            if (attr.SearchFieldType == SearchFieldType.INDEXED && (info.PropertyType == typeof(Vector) || info.PropertyType.BaseType == typeof(Vector)))
            {
                var vectorizer = info.GetCustomAttributes<VectorizerAttribute>().FirstOrDefault();
                if (vectorizer is not null && vectorizer is not FloatVectorizerAttribute && vectorizer is not DoubleVectorizerAttribute)
                {
                    suffix = ".Vector";
                }
            }

            var ret = new List<string> { !string.IsNullOrEmpty(attr.PropertyName) ? $"{attr.PropertyName}{suffix}" : $"{info.Name}{suffix}" };
            if (!string.IsNullOrEmpty(suffix))
            {
                ret.Add("AS");
                ret.Add(!string.IsNullOrEmpty(attr.PropertyName) ? attr.PropertyName : info.Name);
            }

            var innerType = Nullable.GetUnderlyingType(info.PropertyType);
            ret.AddRange(CommonSerialization(attr, innerType ?? info.PropertyType, info));
            return ret.ToArray();
        }

        private static bool IsTypeIndexableArray(Type t) => t == typeof(string[]) || t == typeof(bool[]) || t == typeof(Guid[]) || t == typeof(List<string>) || t == typeof(List<bool>) || t == typeof(List<Guid>);

        private static IEnumerable<string> SerializeIndexFromJsonPaths(PropertyInfo parentInfo, SearchFieldAttribute attribute, string prefix = "$.", string aliasPrefix = "", int remainingDepth = -1)
        {
            var isCollection = false;
            var indexArgs = new List<string>();
            var path = attribute.JsonPath;
            var propertyNames = path!.Split('.').Skip(1).ToArray();
            var type = parentInfo.PropertyType;
            if (type.IsArray)
            {
                type = type.GetElementType();
                isCollection = true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                type = type.GenericTypeArguments.First();
                isCollection = true;
            }

            PropertyInfo propertyInfo = parentInfo;
            foreach (var name in propertyNames)
            {
                var childProperty = type.GetProperty(name);
                if (childProperty == null)
                {
                    throw new RedisIndexingException($"{path} not found in {parentInfo.Name} object graph.");
                }

                propertyInfo = childProperty;
                type = childProperty.PropertyType;
            }

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (!IsComplexType(underlyingType))
            {
                var arrayStr = isCollection ? "[*]" : string.Empty;
                indexArgs.Add($"{prefix}{parentInfo.Name}{arrayStr}{path.Substring(1)}");
                indexArgs.Add("AS");
                indexArgs.Add($"{aliasPrefix}{parentInfo.Name}_{string.Join("_", propertyNames)}");
                indexArgs.AddRange(CommonSerialization(attribute, underlyingType, propertyInfo));
            }
            else
            {
                int cascadeDepth = remainingDepth == -1 ? attribute.CascadeDepth : remainingDepth;
                if (cascadeDepth > 0)
                {
                    foreach (var property in propertyInfo.PropertyType.GetProperties())
                    {
                        var pathPrefix = $"{prefix}{parentInfo.Name}{path.Substring(1)}.";
                        var alias = $"{aliasPrefix}{parentInfo.Name}_{string.Join("_", propertyNames)}_";
                        indexArgs.AddRange(property.SerializeArgsJson(cascadeDepth - 1, pathPrefix, alias));
                    }
                }
            }

            return indexArgs;
        }

        private static string GetSearchFieldType(Type declaredType, SearchFieldAttribute attr, PropertyInfo propertyInfo)
        {
            var typeEnum = attr.SearchFieldType;
            if (typeEnum != SearchFieldType.INDEXED)
            {
                return typeEnum.ToString();
            }

            if (declaredType == typeof(GeoLoc))
            {
                return "GEO";
            }

            if (declaredType == typeof(Vector) || declaredType.BaseType == typeof(Vector))
            {
                return "VECTOR";
            }

            if (declaredType.IsEnum)
            {
                return propertyInfo.GetCustomAttributes(typeof(JsonConverterAttribute)).FirstOrDefault() is JsonConverterAttribute converter && converter.ConverterType == typeof(JsonStringEnumConverter) ? "TAG" : "NUMERIC";
            }

            return TypeDeterminationUtilities.IsNumeric(declaredType) ? "NUMERIC" : "TAG";
        }

        private static bool IsEnumTypeFlags(Type type) => type.GetCustomAttributes(typeof(FlagsAttribute), false).Any();

        private static IEnumerable<string> VectorSerialization(IndexedAttribute vectorAttribute, PropertyInfo propertyInfo)
        {
            var vectorizer = propertyInfo.GetCustomAttributes<VectorizerAttribute>().FirstOrDefault();
            if (vectorizer is null)
            {
                throw new InvalidOperationException("Indexed vector fields must provide a vectorizer.");
            }

            yield return vectorAttribute.Algorithm.AsRedisString();
            yield return vectorAttribute.NumArgs.ToString();
            yield return "TYPE";
            yield return vectorizer.VectorType.AsRedisString();

            yield return "DIM";
            yield return vectorizer.Dim.ToString();
            yield return "DISTANCE_METRIC";
            yield return vectorAttribute.DistanceMetric.AsRedisString();
            if (vectorAttribute.InitialCapacity != default)
            {
                yield return "INITIAL_CAP";
                yield return vectorAttribute.InitialCapacity.ToString();
            }

            if (vectorAttribute.Algorithm == VectorAlgorithm.FLAT)
            {
                if (vectorAttribute.BlockSize != default)
                {
                    yield return "BLOCK_SIZE";
                    yield return vectorAttribute.BlockSize.ToString();
                }
            }
            else if (vectorAttribute.Algorithm == VectorAlgorithm.HNSW)
            {
                if (vectorAttribute.M != default)
                {
                    yield return "M";
                    yield return vectorAttribute.M.ToString();
                }

                if (vectorAttribute.EfConstructor != default)
                {
                    yield return "EF_CONSTRUCTION";
                    yield return vectorAttribute.EfConstructor.ToString();
                }

                if (vectorAttribute.EfRuntime != default)
                {
                    yield return "EF_RUNTIME";
                    yield return vectorAttribute.EfRuntime.ToString();
                }

                if (vectorAttribute.Epsilon != default)
                {
                    yield return "EPSILON";
                    yield return vectorAttribute.Epsilon.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private static string[] CommonSerialization(SearchFieldAttribute attr, Type declaredType, PropertyInfo propertyInfo)
        {
            var searchFieldType = GetSearchFieldType(declaredType, attr, propertyInfo);
            var ret = new List<string> { searchFieldType };
            if (attr is SearchableAttribute text)
            {
                if (text.NoStem)
                {
                    ret.Add("NOSTEM");
                }

                if (!string.IsNullOrEmpty(text.PhoneticMatcher))
                {
                    ret.Add("PHONETIC");
                    ret.Add(text.PhoneticMatcher);
                }

                if (Math.Abs(text.Weight - 1) > .0001)
                {
                    ret.Add("WEIGHT");
                    ret.Add(text.Weight.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (searchFieldType == "TAG" && attr is IndexedAttribute tag)
            {
                if (tag.Separator != ',' && !declaredType.IsEnum)
                {
                    ret.Add("SEPARATOR");
                    ret.Add(tag.Separator.ToString());
                }
                else if (declaredType.IsEnum && IsEnumTypeFlags(declaredType))
                {
                    ret.Add("SEPARATOR");
                    ret.Add(",");
                }

                if (tag.CaseSensitive)
                {
                    ret.Add("CASESENSITIVE");
                }
            }

            if (searchFieldType == "VECTOR" && attr is IndexedAttribute vector)
            {
                ret.AddRange(VectorSerialization(vector, propertyInfo));
            }

            if (attr.Sortable || attr.Aggregatable)
            {
                ret.Add("SORTABLE");
            }

            if (attr.Sortable && !attr.Normalize)
            {
                ret.Add("UNF");
            }

            return ret.ToArray();
        }
    }
}
