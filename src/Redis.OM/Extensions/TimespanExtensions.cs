using System;
using System.Globalization;

namespace Redis.OM;

/// <summary>
/// Extension methods for Timespans.
/// </summary>
internal static class TimespanExtensions
{
    /// <summary>
    /// Rounds up total milliseconds as an integer.
    /// </summary>
    /// <param name="ts">the timespan.</param>
    /// <returns>the rounded timespan milliseconds.</returns>
    public static string TotalMillisecondsString(this TimeSpan ts)
    {
        return Math.Ceiling(ts.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
    }
}