using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NRedisPlus.RediSearch
{
    public class GeoLocJsonConverter : JsonConverter<GeoLoc>
    {
        public override GeoLoc Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (str != null)
            {
                var split = str.Split(',');
                if (split.Length == 2)
                {
                    double lon;
                    double lat;
                    if (double.TryParse(split[0], out lon) && double.TryParse(split[1], out lat))
                        return new GeoLoc(lon, lat);
                }
            }            
            throw new FormatException("GeoLoc was not in a valid format to be parsed");
        }

        public override void Write(Utf8JsonWriter writer, GeoLoc value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());            
        }
    }
}
