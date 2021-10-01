using System;
using System.Collections.Generic;

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
        
    }
}