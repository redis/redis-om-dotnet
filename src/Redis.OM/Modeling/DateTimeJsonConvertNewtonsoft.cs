using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Redis.OM.Modeling;

/// <summary>
/// Converter for Newtonsoft.
/// </summary>
internal class DateTimeJsonConvertNewtonsoft : JsonConverter
{
    /// <summary>
    /// Determines is the object is convertable.
    /// </summary>
    /// <param name="objectType">the object type.</param>
    /// <returns>whether it can be converted.</returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
    }

    /// <summary>
    /// writes the object to json.
    /// </summary>
    /// <param name="writer">the writer.</param>
    /// <param name="value">the value.</param>
    /// <param name="serializer">the serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTime dateTime)
        {
            // Convert DateTime to Unix timestamp
            long unixTimestamp = ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
            writer.WriteValue(unixTimestamp);
        }
        else
        {
            writer.WriteNull();
        }
    }

    /// <summary>
    /// reads an object back from json.
    /// </summary>
    /// <param name="reader">the reader.</param>
    /// <param name="objectType">the object type.</param>
    /// <param name="existingValue">the existing value.</param>
    /// <param name="serializer">the serializer.</param>
    /// <returns>The converted object.</returns>
    /// <exception cref="JsonSerializationException">thrown if issue comes up deserializing.</exception>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return objectType == typeof(DateTime?) ? null : DateTime.MinValue;
        }

        if (reader.TokenType == JsonToken.Integer && reader.Value is long unixTimestamp)
        {
            // Convert Unix timestamp back to DateTime
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).UtcDateTime;
        }

        throw new JsonSerializationException("Invalid token type for Unix timestamp conversion.");
    }
}