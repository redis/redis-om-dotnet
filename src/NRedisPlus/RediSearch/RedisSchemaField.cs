using NRedisPlus.RediSearch.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using NRedisPlus.Schema;

namespace NRedisPlus.RediSearch
{
    internal static class RedisSchemaField
    {
        

        private static string GetSearchFieldType(SearchFieldType typeEnum, Type declaredtype)
        {
            if (typeEnum != SearchFieldType.INDEXED)
            {
                return typeEnum.ToString();
            }
            if (declaredtype == typeof(GeoLoc))
            {
                return "GEO";
            }
            return TypeDeterminationUtilities.IsNumeric(declaredtype) ? "NUMERIC" : "TAG";
        }
        
        private static string[] CommonSerialization(SearchFieldAttribute attr, Type declaredType)
        {
            var searchFieldType = GetSearchFieldType(attr.SearchFieldType, declaredType);
            var ret = new List<string> {searchFieldType};
            if(attr is SearchableAttribute text)
            {
                if (text.NoStem) ret.Add("NOSTEM");
                if (!string.IsNullOrEmpty(text.PhoneticMatcher))
                {
                    ret.Add("PHONETIC");
                    ret.Add(text.PhoneticMatcher);
                }
                if (text.Weight != 1)
                {
                    ret.Add("WEIGHT");
                    ret.Add(text.Weight.ToString());
                }
            }

            if (searchFieldType == "TAG" && attr is IndexedAttribute tag)
            {
                if (tag.Separator != ',')
                {
                    ret.Add("SEPARATOR");
                    ret.Add(tag.Separator.ToString());
                }

                if (tag.CaseSensitive) ret.Add("CASESENSITIVE");
            }
            if (attr.Sortable || attr.Aggregatable) ret.Add("SORTABLE");
            if (attr.Sortable && !attr.Normalize) ret.Add("UNF");
            return ret.ToArray();
        }

        internal static string[] SerializeArgsJson(this PropertyInfo info)
        {
            var attr = Attribute.GetCustomAttribute(info, typeof(SearchFieldAttribute)) as SearchFieldAttribute;
            if (attr == null)
                return Array.Empty<string>();

            var ret = new List<string>
            {
                !string.IsNullOrEmpty(attr.PropertyName) ? $"$.{attr.PropertyName}" : $"$.{info.Name}",
                "AS",
                !string.IsNullOrEmpty(attr.PropertyName) ? attr.PropertyName : info.Name
            };
            var innerType = Nullable.GetUnderlyingType(info.PropertyType);
            ret.AddRange(CommonSerialization(attr, innerType ?? info.PropertyType));
            return ret.ToArray();
        }

        internal static string[] SerializeArgs(this PropertyInfo info)
        {
            var attr = Attribute.GetCustomAttribute(info, typeof(SearchFieldAttribute)) as SearchFieldAttribute;
            if (attr == null)
                return Array.Empty<string>();
            
            var ret = new List<string> {!string.IsNullOrEmpty(attr.PropertyName) ? attr.PropertyName : info.Name};
            var innerType = Nullable.GetUnderlyingType(info.PropertyType);
            ret.AddRange(CommonSerialization(attr, innerType ?? info.PropertyType));
            return ret.ToArray();
        }
    }
}
