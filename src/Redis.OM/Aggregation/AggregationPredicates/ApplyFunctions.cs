using System;
using Redis.OM.Modeling;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// Functions to use within apply expressions. Minimal implementations provided for sanity checks, but this should
    /// be used primarily within expressions.
    /// </summary>
    public static class ApplyFunctions
    {
        private const double RADIANTTODEGREESCONST = Math.PI / 180.0;

        /// <summary>
        /// checks if the field exists on the object in redis.
        /// </summary>
        /// <param name="field">the field to check.</param>
        /// <returns>whether the field exists or not.</returns>
        public static bool Exists(object field)
        {
            return true;
        }

        /// <summary>
        /// Formats the unix timestamp into a string timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>a formatted timestamp of %FT%TZ.</returns>
        public static string FormatTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.ToString("%FT%TZ");
        }

        /// <summary>
        /// Formats the unix timestamp into a string timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="format">The format to use <see href="http://strftime.org/">strftime</see>.</param>
        /// <returns>a formatted timestamp of %FT%TZ.</returns>
        public static string FormatTimestamp(long timestamp, string format)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime.ToString(format);
        }

        /// <summary>
        /// parses a unix timestamp from the provided string.
        /// </summary>
        /// <param name="timestamp">the timestamp in %FT%TZ format.</param>
        /// <returns>the unix timestamp.</returns>
        public static long ParseTime(string timestamp)
        {
            return new DateTimeOffset(DateTime.Parse(timestamp)).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Formats the unix timestamp into a string timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <param name="format">The format to use <see href="http://strftime.org/">strftime</see>.</param>
        /// <returns>a formatted timestamp of %FT%TZ.</returns>
        public static long ParseTime(string timestamp, string format)
        {
            return ParseTime(timestamp);
        }

        /// <summary>
        /// Rounds the timestamp to midnight of the current day.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>the timestamp rounded to midnight.</returns>
        public static long Day(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, 0, 0, 0)).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Rounds the timestamp to the beginning of the current hour.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>the timestamp rounded to the current hour.</returns>
        public static long Hour(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0)).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Rounds the timestamp to the beginning of the current minute.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>the timestamp rounded to the current minute.</returns>
        public static long Minute(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0)).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Rounds the timestamp to the beginning of the current month.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>The timestamp rounded to the beginning of the current month.</returns>
        public static long Month(long timestamp)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return new DateTimeOffset(new DateTime(time.Year, time.Month, 0, 0, 0, 0)).ToUnixTimeSeconds();
        }

        /// <summary>
        /// get's the day of the week from the timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>the day of the week Sunday=0.</returns>
        public static long DayOfWeek(long timestamp)
        {
            return (long)DateTimeOffset.FromUnixTimeSeconds(timestamp).DayOfWeek;
        }

        /// <summary>
        /// Gets the day of the month from the timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>The day of the month.</returns>
        public static long DayOfMonth(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).Day;
        }

        /// <summary>
        /// Gets the day of the year from the timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>The day of the year.</returns>
        public static long DayOfYear(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DayOfYear;
        }

        /// <summary>
        /// Gets the year from the timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>the year.</returns>
        public static long Year(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).Year;
        }

        /// <summary>
        /// Gets the month from the timestamp.
        /// </summary>
        /// <param name="timestamp">the timestamp.</param>
        /// <returns>the month.</returns>
        public static long MonthOfYear(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).Month;
        }

        /// <summary>
        /// calculates the distance between two points in meters.
        /// </summary>
        /// <param name="loc1">first location.</param>
        /// <param name="loc2">second location.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(GeoLoc loc1, GeoLoc loc2) =>
            GeoDistance(loc1.Longitude, loc1.Latitude, loc2.Longitude, loc2.Latitude);

        /// <summary>
        /// calculates the distance between two points in meters.
        /// </summary>
        /// <param name="loc1">first location.</param>
        /// <param name="loc2Str">second location.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(GeoLoc loc1, string loc2Str)
        {
            var loc2 = GeoLoc.Parse(loc2Str);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// calculates the distance between two points in meters.
        /// </summary>
        /// <param name="loc1">first location.</param>
        /// <param name="lon2">Second location's longitude.</param>
        /// <param name="lat2">Second location's latitude.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(GeoLoc loc1, double lon2, double lat2)
        {
            var loc2 = new GeoLoc(lon2, lat2);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// calculates the distance between two points in meters.
        /// </summary>
        /// <param name="loc1Str">First location.</param>
        /// <param name="loc2">Second location.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(string loc1Str, GeoLoc loc2)
        {
            var loc1 = GeoLoc.Parse(loc1Str);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// calculates the distance between two points in meters.
        /// </summary>
        /// <param name="loc1Str">First location.</param>
        /// <param name="loc2Str">Second location.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(string loc1Str, string loc2Str)
        {
            var loc1 = GeoLoc.Parse(loc1Str);
            var loc2 = GeoLoc.Parse(loc2Str);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// calculates the distance between two points in meters.
        /// </summary>
        /// <param name="loc1Str">First location.</param>
        /// <param name="lon2">Second location longitude.</param>
        /// <param name="lat2">Second location latitude.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(string loc1Str, double lon2, double lat2)
        {
            var loc1 = GeoLoc.Parse(loc1Str);
            var loc2 = new GeoLoc(lon2, lat2);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// Calculates the distance between two points in meters.
        /// </summary>
        /// <param name="lon1">first location's longitude.</param>
        /// <param name="lat1">first location's latitude.</param>
        /// <param name="loc2">second location.</param>
        /// <returns>the distance in meters.</returns>
        public static double GeoDistance(double lon1, double lat1, GeoLoc loc2)
        {
            var loc1 = new GeoLoc(lon1, lat1);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// Calculates the distance between two points in meters.
        /// </summary>
        /// <param name="lon1">first location's longitude.</param>
        /// <param name="lat1">first location's latitude.</param>
        /// <param name="loc2Str">second location.</param>
        /// <returns>distance in meters.</returns>
        public static double GeoDistance(double lon1, double lat1, string loc2Str)
        {
            var loc1 = new GeoLoc(lon1, lat1);
            var loc2 = GeoLoc.Parse(loc2Str);
            return GeoDistance(loc1, loc2);
        }

        /// <summary>
        /// Calculates the distance between two points. Minimal implementation taken from Stackoverflow. https://stackoverflow.com/a/51839058/7299345.
        /// </summary>
        /// <param name="longitude">first location longitude.</param>
        /// <param name="latitude">first location latitude.</param>
        /// <param name="otherLongitude">second location longitude.</param>
        /// <param name="otherLatitude">second location's latitude.</param>
        /// <returns>distance in meters.</returns>
        public static double GeoDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * RADIANTTODEGREESCONST;
            var num1 = longitude * RADIANTTODEGREESCONST;
            var d2 = otherLatitude * RADIANTTODEGREESCONST;
            var num2 = (otherLongitude * RADIANTTODEGREESCONST) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + (Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0));

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}
