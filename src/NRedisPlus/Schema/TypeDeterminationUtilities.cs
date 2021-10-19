using System;
using System.Collections.Generic;
using NRedisPlus.RediSearch;

namespace NRedisPlus.Schema
{
    /// <summary>
    /// Utilities to determine the type of a thing.
    /// </summary>
    internal static class TypeDeterminationUtilities
    {
        private static readonly HashSet<Type> NumericTypes = new ()
        {
            typeof(int),
            typeof(double),
            typeof(decimal),
            typeof(long),
            typeof(short),
            typeof(sbyte),
            typeof(byte),
            typeof(ulong),
            typeof(uint),
            typeof(ushort),
            typeof(float),
        };

        /// <summary>
        /// Is the type numeric.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Whether the type is numeric or not.</returns>
        internal static bool IsNumeric(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return NumericTypes.Contains(underlyingType);
            }

            return NumericTypes.Contains(type);
        }

        /// <summary>
        /// Determine the SearchFieldType for the given type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>The SearchFieldType.</returns>
        /// <exception cref="ArgumentException">Thrown if search field is of an unrecognized type.</exception>
        internal static SearchFieldType GetSearchFieldType(Type type)
        {
            if (type == typeof(GeoLoc) || type == typeof(GeoLoc?))
            {
                return SearchFieldType.GEO;
            }

            if (IsNumeric(type))
            {
                return SearchFieldType.NUMERIC;
            }

            if (type == typeof(string))
            {
                return SearchFieldType.TAG;
            }

            throw new ArgumentException("Unrecognized Index type, can only index numerics, GeoLoc, or String");
        }
    }
}
