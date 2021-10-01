using System;

namespace NRedisPlus.RediSearch
{
    public struct GeoLoc
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public GeoLoc(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }
        public override string ToString()
        {
            return $"{Longitude},{Latitude}";
        }
        public static GeoLoc Parse(string s)
        {
            var arr = s.Split(',');
            if(arr.Length == 2)
            {
                double lon;
                double lat;
                if (double.TryParse(arr[0], out lon) && double.TryParse(arr[1], out lat))
                    return new GeoLoc(lon,lat);
            }
            throw new ArgumentException("GeoLoc string must be a string in the format longitude,latitude");
        }
    }
}
