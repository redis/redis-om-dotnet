using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Redis.OM.Modeling;

namespace Redis.OM
{
    /// <summary>
    /// Configurable settings for how serialization and deserialization is handled.
    /// </summary>
    public static class RedisSerializationSettings
    {
        /// <summary>
        /// <see cref="JsonSerializerOptions"/> used when serializing.
        /// </summary>
        public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        static RedisSerializationSettings()
        {
            JsonSerializerOptions.Converters.Add(new GeoLocJsonConverter());
            JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
        }

        /// <summary>
        /// Gets default/assumed timezone used by redis when deserializing datetimes.
        /// </summary>
        public static TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Local;

        /// <summary>
        /// Set the default/assumed <see cref="TimeZoneInfo"/> for deserialization to <see cref="TimeZoneInfo.Utc"/> instead of <see cref="TimeZoneInfo.Local"/>.
        /// </summary>
        public static void UseUtcTime()
        {
            TimeZone = TimeZoneInfo.Utc;
        }

        /// <summary>
        /// Set the default/assumed <see cref="TimeZoneInfo"/> for deserialization to the default of <see cref="TimeZoneInfo.Local"/>.
        /// </summary>
        public static void UseLocalTime()
        {
            TimeZone = TimeZoneInfo.Local;
        }
    }
}