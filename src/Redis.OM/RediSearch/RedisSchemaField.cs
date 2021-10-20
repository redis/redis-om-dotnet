using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Redis.OM.RediSearch.Attributes;
using Redis.OM.Schema;

namespace Redis.OM.RediSearch
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
        /// <returns>The create index args for the schema field for JSON.</returns>
        internal static string[] SerializeArgsJson(this PropertyInfo info)
        {
            if (Attribute.GetCustomAttribute(info, typeof(SearchFieldAttribute)) is not SearchFieldAttribute attr)
            {
                return Array.Empty<string>();
            }

            var ret = new List<string>
            {
                !string.IsNullOrEmpty(attr.PropertyName) ? $"$.{attr.PropertyName}" : $"$.{info.Name}",
                "AS",
                !string.IsNullOrEmpty(attr.PropertyName) ? attr.PropertyName : info.Name,
            };
            var innerType = Nullable.GetUnderlyingType(info.PropertyType);
            ret.AddRange(CommonSerialization(attr, innerType ?? info.PropertyType));
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
            ret.AddRange(CommonSerialization(attr, innerType ?? info.PropertyType));
            return ret.ToArray();
        }

        private static string GetSearchFieldType(SearchFieldType typeEnum, Type declaredType)
        {
            if (typeEnum != SearchFieldType.INDEXED)
            {
                return typeEnum.ToString();
            }

            if (declaredType == typeof(GeoLoc))
            {
                return "GEO";
            }

            return TypeDeterminationUtilities.IsNumeric(declaredType) ? "NUMERIC" : "TAG";
        }

        private static string[] CommonSerialization(SearchFieldAttribute attr, Type declaredType)
        {
            var searchFieldType = GetSearchFieldType(attr.SearchFieldType, declaredType);
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
                if (tag.Separator != ',')
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
