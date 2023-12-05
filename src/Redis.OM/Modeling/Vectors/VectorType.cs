using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Type of Vector.
    /// </summary>
    public enum VectorType
    {
        /// <summary>
        /// Float 32s.
        /// </summary>
        FLOAT32,

        /// <summary>
        /// Float 64s.
        /// </summary>
        FLOAT64,
    }

    /// <summary>
    /// Extensions for VectorType.
    /// </summary>
    internal static class VectorTypeExtensions
    {
        /// <summary>
        /// Gets the Vector type as a Redis usable string.
        /// </summary>
        /// <param name="vectorType">The Vector type.</param>
        /// <returns>A Redis Usable string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if illegal value for Vector type is encountered.</exception>
        internal static string AsRedisString(this VectorType vectorType)
        {
            return vectorType switch
            {
                VectorType.FLOAT32 => "FLOAT32",
                VectorType.FLOAT64 => "FLOAT64",
                _ => throw new ArgumentOutOfRangeException(nameof(vectorType))
            };
        }
    }
}