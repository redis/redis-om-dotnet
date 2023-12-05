using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// ignores the Json Score field.
    /// </summary>
    internal class JsonScoreConverter : JsonConverter<object>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(double) || typeToConvert == typeof(double?);

        /// <inheritdoc/>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return -1.0;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(-1.0);
        }
    }
}