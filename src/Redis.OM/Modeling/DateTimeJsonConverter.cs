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
            var val = reader.GetString();
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime.AddMilliseconds(long.Parse(val!)), TimeZoneInfo.Local);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(new DateTimeOffset(value).ToUnixTimeMilliseconds().ToString());
        }
    }
}