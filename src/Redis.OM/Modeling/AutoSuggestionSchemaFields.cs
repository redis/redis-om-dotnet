using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// A utility class for serializing objects into Redis.
    /// </summary>
    internal static class AutoSuggestionSchemaFields
    {
        /// <summary>
        /// Gets or sets the string value for the AutoSuggestion.
        /// </summary>
        public static string? IndexKey { get; set; }

        /// <summary>
        /// Gets or sets the string value for the AutoSuggestion.
        /// </summary>
        public static string? String { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public static string? Score { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public static AutoSuggestionOptionalParameters? OptionalParameters { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public static string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the float Score for the AutoSuggestion.
        /// </summary>
        public static string? Payload { get; set; }

        /// <summary>
        /// Pull out the AutoSuggestion attribute from a Type.
        /// </summary>
        /// <param name="type">The type to pull the attribute out from.</param>
        /// <returns>A documentation attribute.</returns>
        internal static AutoSuggestionAttribute? GetObjectDefinition(this Type type)
        {
            return Attribute.GetCustomAttribute(
                type,
                typeof(AutoSuggestionAttribute)) as AutoSuggestionAttribute;
        }

        /// <summary>
        /// Serialize the Index.
        /// </summary>
        /// <param name="type">The type to be indexed.</param>
        /// <exception cref="InvalidOperationException">Thrown if type provided is not decorated with a RedisObjectDefinitionAttribute.</exception>
        /// <returns>An array of strings (the serialized args for redis).</returns>
        internal static string[] SerializeSuggestions(this Type type)
        {
            var objAttribute = Attribute.GetCustomAttribute(
                type,
                typeof(AutoSuggestionAttribute)) as AutoSuggestionAttribute;
            if (objAttribute == null)
            {
                throw new InvalidOperationException($"Type being indexed must be decorated " +
                    $"with an RedisObjectDefinitionAttribute, none found on provided type:{type.Name}");
            }

            var args = new List<string>();
            if (string.IsNullOrEmpty(objAttribute.Key))
            {
                args.Add($"SUG:{type.Name.ToLower()}:{type.ReflectedType}");
            }
            else
            {
                args.Add(objAttribute.Key!);
            }

            foreach (FieldInfo field in type.GetFields())
            {
                if (field.Attributes.Equals(typeof(AutoSuggestionAttribute)))
                {
                    IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());

                    foreach (PropertyInfo prop in props)
                    {
                        object propValue = prop.GetValue(type, null);
                        args.Add(propValue.ToString());
                    }
                }
            }

            args.Add("1.0");
            if (objAttribute.OptionalParameters == AutoSuggestionOptionalParameters.INCR)
            {
                args.Add("INCR");
            }

            if (objAttribute.Payload != null)
            {
                args.Add("PAYLOAD");
            }

            return args.ToArray();
        }
    }
}
