using System;

namespace NRedisPlus.Schema
{
    /// <summary>
    /// A structure representing a point on the globe by it's longitude and latitude.
    /// </summary>
    public readonly struct GeoLoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoLoc"/> struct.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        public GeoLoc(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        /// <summary>
        /// Gets the longitude.
        /// </summary>
        public double Longitude { get; }

        /// <summary>
        /// Gets the latitude.
        /// </summary>
        public double Latitude { get; }

        /// <summary>
        /// Parses a Geolocation from a string.
        /// </summary>
        /// <param name="geolocString">the string representation of a geoloc.</param>
        /// <returns>a geoloc parsed from the string.</returns>
        /// <exception cref="ArgumentException">thrown if the geoloc could not be parsed from the string.</exception>
        public static GeoLoc Parse(string geolocString)
        {
            var arr = geolocString.Split(',');
            if (arr.Length == 2)
            {
                if (double.TryParse(arr[0], out var lon) && double.TryParse(arr[1], out var lat))
                {
                    return new GeoLoc(lon, lat);
                }
            }

            throw new ArgumentException("GeoLoc string must be a string in the format longitude,latitude");
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Longitude},{Latitude}";
        }
    }
}
