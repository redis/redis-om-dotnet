using System;
using System.Collections.Generic;
using System.Globalization;

namespace NRedisPlus.RediSearch.Query
{
    /// <summary>
    /// A geographic filter.
    /// </summary>
    public class RedisGeoFilter : QueryOption
    {
        private readonly string _fieldName;
        private readonly double _longitude;
        private readonly double _latitude;
        private readonly double _radius;
        private readonly GeoLocDistanceUnit _unit;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisGeoFilter"/> class.
        /// </summary>
        /// <param name="field">the field to filter.</param>
        /// <param name="longitude">the longitude.</param>
        /// <param name="latitude">the latitude.</param>
        /// <param name="radius">radius.</param>
        /// <param name="unit">the unit.</param>
        public RedisGeoFilter(string field, double longitude, double latitude, double radius, GeoLocDistanceUnit unit)
        {
            _fieldName = field;
            _longitude = longitude;
            _latitude = latitude;
            _radius = radius;
            _unit = unit;
        }

        /// <inheritdoc/>
        public override IEnumerable<string> QueryText
        {
            get
            {
                var unitString = _unit switch
                {
                    GeoLocDistanceUnit.Feet => "ft",
                    GeoLocDistanceUnit.Kilometers => "km",
                    GeoLocDistanceUnit.Meters => "m",
                    GeoLocDistanceUnit.Miles => "mi",
                    _ => throw new Exception("Invalid unit")
                };
                return new[]
                {
                    "GEOFILTER",
                    _fieldName,
                    _longitude.ToString(CultureInfo.InvariantCulture),
                    _latitude.ToString(CultureInfo.InvariantCulture),
                    _radius.ToString(CultureInfo.InvariantCulture),
                    unitString,
                };
            }
        }
    }
}
