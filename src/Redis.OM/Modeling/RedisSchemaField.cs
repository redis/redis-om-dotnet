using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// utility methods for serializing schema fields.
    /// </summary>
    internal static class RedisSchemaField
    {
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
                if (attr.JsonPath != null)
                {
                    ret.AddRange(SerializeIndexFromJsonPaths(info, attr));
                }
                else
                {
                    var innerType = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;

                    if (!TypeDeterminationUtilities.IsNumeric(innerType)
                        && innerType != typeof(string)
                        && innerType != typeof(GeoLoc)
                        && innerType != typeof(Ulid)
                        && innerType != typeof(bool)
                        && innerType != typeof(Guid)
                        && !innerType.IsEnum
                        && !IsTypeIndexableArray(innerType))
                    {
                        int cascadeDepth = remainingDepth == -1 ? attr.CascadeDepth : remainingDepth;
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
                        ret.Add(!string.IsNullOrEmpty(attr.PropertyName) ? $"{pathPrefix}{attr.PropertyName}{pathPrefix}" : $"{pathPrefix}{info.Name}{pathPostFix}");
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
            if (attr == null)
            {
                return Array.Empty<string>();
            }

            var ret = new List<string> { !string.IsNullOrEmpty(attr.PropertyName) ? attr.PropertyName : info.Name };
            var innerType = Nullable.GetUnderlyingType(info.PropertyType);
            ret.AddRange(CommonSerialization(attr, innerType ?? info.PropertyType, info));
            return ret.ToArray();
        }

        private static bool IsTypeIndexableArray(Type t) => t == typeof(string[]) || t == typeof(bool[]) || t == typeof(List<string>) || t == typeof(List<bool>);

        private static IEnumerable<string> SerializeIndexFromJsonPaths(PropertyInfo parentInfo, SearchFieldAttribute attribute, string prefix = "$.")
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

            var arrayStr = isCollection ? "[*]" : string.Empty;
            indexArgs.Add($"{prefix}{parentInfo.Name}{arrayStr}{path.Substring(1)}");
            indexArgs.Add("AS");
            indexArgs.Add($"{parentInfo.Name}_{string.Join("_", propertyNames)}");
            var underlyingType = Nullable.GetUnderlyingType(type);
            indexArgs.AddRange(CommonSerialization(attribute, underlyingType ?? type, propertyInfo));
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

            if (declaredType.IsEnum)
            {
                return propertyInfo.GetCustomAttributes(typeof(JsonConverterAttribute)).FirstOrDefault() is JsonConverterAttribute converter && converter.ConverterType == typeof(JsonStringEnumConverter) ? "TAG" : "NUMERIC";
            }

            return TypeDeterminationUtilities.IsNumeric(declaredType) ? "NUMERIC" : "TAG";
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

                if (tag.CaseSensitive)
                {
                    ret.Add("CASESENSITIVE");
                }
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
