using System;
using System.Collections.Generic;
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
        /// Serialize .
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
                args.Add($"sugg:{type.Name.ToLower()}");
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

        /// <summary>
        /// Serialize .
        /// </summary>
        /// <param name="type">The type to be indexed.</param>
        /// <param name="prefix">prefix to complete on.</param>
        /// <param name="fuzzy">Optional type performs a fuzzy prefix search.</param>
        /// <param name="max">Optional type limits the results to a maximum of num (default: 5).</param>
        /// <param name="withscores">Optional type also returns the score of each suggestion.</param>
        /// <param name="withpayloads">Optional type returns optional payloads saved along with the suggestions.</param>
        /// <exception cref="InvalidOperationException">Thrown if type provided is not decorated with a RedisObjectDefinitionAttribute.</exception>
        /// <returns>An array of strings (the serialized args for redis).</returns>
        internal static string[] SerializeGetSuggestions(this Type type, string prefix = " ", bool? fuzzy = false, int? max = 0, bool? withscores = false, bool? withpayloads = false)
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
                args.Add($"sugg:{type.Name.ToLower()}");
            }
            else
            {
                args.Add(objAttribute.Key!);
            }

            args.Add(prefix);
            if (fuzzy is true)
            {
                args.Add("FUZZY");
            }

            if (max > 0)
            {
                args.Add("MAX");
                args.Add(max.ToString());
            }

            if (withscores is true)
            {
                args.Add("WITHSCORES");
            }

            if (withpayloads is true)
            {
                args.Add("WITHPAYLOADS");
            }

            return args.ToArray();
        }
    }
}
