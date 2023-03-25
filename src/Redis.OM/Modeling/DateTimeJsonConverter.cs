using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// DateTime converter for JSON serialization.
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        /// <inheritdoc />
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long val = reader.TokenType == JsonTokenType.String ? long.Parse(reader.GetString() !) : reader.GetInt64();

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime.AddMilliseconds(val), RedisSerializationSettings.TimeZone);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(new DateTimeOffset(value).ToUnixTimeMilliseconds());
        }
    }
}