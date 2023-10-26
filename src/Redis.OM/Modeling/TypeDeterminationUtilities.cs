using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
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
            typeof(DateTime),
        };

        /// <summary>
        /// Determins whether the collection is a numeric collection.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>whether the enumerable is a numeric enumerable.</returns>
        internal static bool IsNumericEnumerable(in IEnumerable enumerable)
        {
            var type = enumerable.GetType().IsArray ? enumerable.GetType().GetElementType() : enumerable.GetType().GenericTypeArguments[0];
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return NumericTypes.Contains(underlyingType);
        }

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

            throw new ArgumentException("Unrecognized Index type, can only index numerics, GeoLoc, strings, ULIDs, GUIDs, Enums, and Booleans");
        }

        /// <summary>
        /// Determines SearchFieldType of provided property info.
        /// </summary>
        /// <param name="info">The PropertyInfo to check.</param>
        /// <returns>The Search field type.</returns>
        internal static SearchFieldType GetSearchFieldFromEnumProperty(PropertyInfo info) =>
            info.GetCustomAttributes<JsonConverterAttribute>().FirstOrDefault() is JsonConverterAttribute converter
            && converter.ConverterType == typeof(JsonStringEnumConverter) ? SearchFieldType.TAG : SearchFieldType.NUMERIC;
    }
}
