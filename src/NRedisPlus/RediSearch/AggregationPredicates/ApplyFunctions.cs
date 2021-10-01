using System;
using System.Collections.Generic;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public static class ApplyFunctions
    {
        public static bool Exists(object field)
        {
            return true;
        }
        public static string FormatTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.ToString("%FT%TZ");
        }

        public static string FormatTimestamp(long timestamp, string format)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.ToString(format);
        }

        public static long ParseTime(string timestamp)
        {
            return new DateTimeOffset(DateTime.Parse(timestamp)).ToUnixTimeSeconds();
        }

        public static long ParseTime(string timestamp, string format)
        {
            return ParseTime(timestamp);
        }

        public static long Day(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, 0, 0, 0)).ToUnixTimeSeconds();
        }

        public static long Hour(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0)).ToUnixTimeSeconds();
        }

        public static long Minute(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0)).ToUnixTimeSeconds();
        }

        public static long Month(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, 0, 0, 0, 0)).ToUnixTimeSeconds();
        }

        public static long DayOfWeek(long timestamp)
        {
            return ((long)DateTimeOffset.FromUnixTimeSeconds(timestamp).DayOfWeek);
        }

        public static long DayOfMonth(long timestamp)
        {
            return ((long)DateTimeOffset.FromUnixTimeSeconds(timestamp).Day);
        }

        public static long DayOfYear(long timestamp)
        {
            return ((long)DateTimeOffset.FromUnixTimeSeconds(timestamp).DayOfYear);
        }

        public static long Year(long timestamp)
        {
            return ((long)DateTimeOffset.FromUnixTimeSeconds(timestamp).Year);
        }

        public static long MonthOfYear(long timestamp)
        {
            return ((long)DateTimeOffset.FromUnixTimeSeconds(timestamp).Month);
        }

        public static double GeoDistance(GeoLoc loc1, GeoLoc loc2) =>
            GeoDistance(loc1.Longitude, loc1.Latitude, loc2.Longitude, loc2.Latitude);

        public static double GeoDistance(GeoLoc loc1, string loc2str)
        {
            var loc2 = GeoLoc.Parse(loc2str);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(GeoLoc loc1, double lon2, double lat2)
        {
            var loc2 = new GeoLoc(lon2, lat2);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(string loc1str, GeoLoc loc2)
        {
            var loc1 = GeoLoc.Parse(loc1str);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(string loc1str, string loc2str)
        {
            var loc1 = GeoLoc.Parse(loc1str);
            var loc2 = GeoLoc.Parse(loc2str);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(string loc1str, double lon2, double lat2)
        {
            var loc1 = GeoLoc.Parse(loc1str);
            var loc2 = new GeoLoc(lon2, lat2);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(double lon1, double lat1, GeoLoc loc2)
        {
            var loc1 = new GeoLoc(lon1, lat1);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(double lon1, double lat1, string loc2str)
        {
            var loc1 = new GeoLoc(lon1, lat1);
            var loc2 = GeoLoc.Parse(loc2str);
            return GeoDistance(loc1, loc2);
        }

        public static double GeoDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}
