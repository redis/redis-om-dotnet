using System;
using System.Globalization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// A structure representing a point on the globe by it's longitude and latitude.
    /// </summary>
    public struct GeoLoc
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
        /// Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; set; }

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
                if (double.TryParse(arr[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var lon) &&
                    double.TryParse(arr[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var lat))
                {
                    return new GeoLoc(lon, lat);
                }
            }

            throw new ArgumentException("GeoLoc string must be a string in the format longitude,latitude");
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Longitude.ToString(CultureInfo.InvariantCulture)},{Latitude.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
