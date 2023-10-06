using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// The Vector distance metric to use.
    /// </summary>
    public enum DistanceMetric
    {
        /// <summary>
        /// Euclidean distance.
        /// </summary>
        L2,

        /// <summary>
        /// Inner Product.
        /// </summary>
        IP,

        /// <summary>
        /// The Cosine distance.
        /// </summary>
        COSINE,
    }

    /// <summary>
    /// Quality of life extensions for distance metrics.
    /// </summary>
    internal static class DistanceMetricExtensions
    {
        /// <summary>
        /// Gets the Distance metric as a Redis usable string.
        /// </summary>
        /// <param name="distanceMetric">The distance Metric.</param>
        /// <returns>A Redis Usable string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">thrown if illegal ordinal encountered.</exception>
        internal static string AsRedisString(this DistanceMetric distanceMetric)
        {
            return distanceMetric switch
            {
                DistanceMetric.L2 => "L2",
                DistanceMetric.IP => "IP",
                DistanceMetric.COSINE => "COSINE",
                _ => throw new ArgumentOutOfRangeException(nameof(distanceMetric))
            };
        }
    }
}