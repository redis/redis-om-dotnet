using System;
using System.Collections.Generic;
using NRedisPlus.RediSearch;

namespace NRedisPlus.Schema
{
    public static class TypeDeterminationUtilities
    {
        public static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(int), typeof(double), typeof(decimal),
            typeof(long), typeof(short), typeof(sbyte),
            typeof(byte), typeof(ulong), typeof(ushort),
            typeof(uint), typeof(float)
        };
        public static bool IsNumeric(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null) return NumericTypes.Contains(underlyingType);
            return NumericTypes.Contains(type);
        }

        public static SearchFieldType GetSearchFieldType(Type type)
        {
            if (type == typeof(GeoLoc) || type == typeof(GeoLoc?))
                return SearchFieldType.GEO;
            if (IsNumeric(type))
                return SearchFieldType.NUMERIC;
            if (type == typeof(string) || type == typeof(String))
                return SearchFieldType.TAG;
            throw new ArgumentException("Unrecognized Index type, can only index numerics, GeoLoc, or String");
        }

    }
}