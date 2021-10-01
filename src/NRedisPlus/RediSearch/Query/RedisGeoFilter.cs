using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class RedisGeoFilter : QueryOption
    {
        private string _fieldName;
        private double _longitude;
        private double _latitutde;
        private double _radius; 
        private GeoLocDistanceUnit _unit { get; set; }

        public RedisGeoFilter(string field, double longitude, double latitude, double radius, GeoLocDistanceUnit unit)
        {
            _fieldName = field;
            _longitude = longitude;
            _latitutde = latitude;
            _radius = radius;
            _unit = unit;
        }

        public override string[] QueryText { get
            {
                var unitString = _unit switch
                {
                    GeoLocDistanceUnit.Feet => "ft",
                    GeoLocDistanceUnit.Kilometers => "km",
                    GeoLocDistanceUnit.Meters => "m",
                    GeoLocDistanceUnit.Miles => "mi",
                    _ => throw new Exception("Invalid unit")
                };
                return new string[]
                {
                    "GEOFILTER",
                    _fieldName,
                    _longitude.ToString(),
                    _latitutde.ToString(),
                    _radius.ToString(),
                    unitString
                };
            } 
        }
    }
}
