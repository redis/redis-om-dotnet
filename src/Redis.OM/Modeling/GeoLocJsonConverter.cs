using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Json Converter for converting <see cref="GeoLoc"/> to and from JSON.
    /// </summary>
    public class GeoLocJsonConverter : JsonConverter<GeoLoc>
    {
        /// <summary>
        /// Parse JSON into a GeoLoc.
        /// </summary>
        /// <param name="reader">the reader.</param>
        /// <param name="typeToConvert">the type to convert.</param>
        /// <param name="options">the options.</param>
        /// <returns>A geoloc parsed from json.</returns>
        /// <exception cref="FormatException">thrown if geoloc not in valid format for parsing.</exception>
        public override GeoLoc Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (str != null)
            {
                var split = str.Split(',');
                if (split.Length == 2)
                {
                    if (double.TryParse(split[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var lon) &&
                        double.TryParse(split[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var lat))
                    {
                        return new GeoLoc(lon, lat);
                    }
                }
            }

            throw new FormatException("GeoLoc was not in a valid format to be parsed");
        }

        /// <summary>
        /// Writes the <see cref="GeoLoc"/> to a string for JSON.
        /// </summary>
        /// <param name="writer">the writer.</param>
        /// <param name="value">the <see cref="GeoLoc"/>.</param>
        /// <param name="options">The options.</param>
        public override void Write(Utf8JsonWriter writer, GeoLoc value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
