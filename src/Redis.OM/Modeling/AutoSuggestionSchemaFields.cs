using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// A utility class for serializing objects into Redis.
    /// </summary>
    internal static class AutoSuggestionSchemaFields
    {
        private static readonly JsonSerializerOptions Options = new ()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

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
        /// <param name="value">string value for suggestion.</param>
        /// <param name="score">score for the given string.</param>
        /// <param name="increment">whether increment the score value.</param>
        /// <param name="jsonpayload">jsonpayload.</param>
        /// <exception cref="InvalidOperationException">Thrown if type provided is not decorated with a RedisObjectDefinitionAttribute.</exception>
        /// <returns>An array of strings (the serialized args for redis).</returns>
        internal static string[] SerializeSuggestions(this Type type, string value = "", float? score = null, bool increment = false, object? jsonpayload = null)
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
                args.Add($"SUG:{type.Name.ToLower()}");
            }
            else
            {
                args.Add(objAttribute.Key!);
            }

            args.Add(value);
            args.Add(score.ToString());
            if (increment is true)
            {
                args.Add("INCR");
            }

            if (objAttribute.Payload && jsonpayload != null)
            {
                args.Add("PAYLOAD");
                var json = JsonSerializer.Serialize(jsonpayload, Options);
                args.Add(json);
            }

            return args.ToArray();
        }
    }
}
