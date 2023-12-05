using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// The Vector Algorithm.
    /// </summary>
    public enum VectorAlgorithm
    {
        /// <summary>
        /// Uses a brute force algorithm to find nearest neighbors.
        /// </summary>
        FLAT = 0,

        /// <summary>
        /// Uses the Hierarchical Small World Algorithm to build an efficient graph structure to
        /// retrieve approximate nearest neighbors
        /// </summary>
        HNSW = 1,
    }

    /// <summary>
    /// Quality of life functions for VectorAlgorithm enum.
    /// </summary>
    internal static class VectorAlgorithmExtensions
    {
        /// <summary>
        /// Returns the algorithm as a Redis Serialized String.
        /// </summary>
        /// <param name="algorithm">The algorithm to use.</param>
        /// <returns>The algorithm's name.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid Algorithm is passed.</exception>
        internal static string AsRedisString(this VectorAlgorithm algorithm)
        {
            return algorithm switch
            {
                VectorAlgorithm.FLAT => "FLAT",
                VectorAlgorithm.HNSW => "HNSW",
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
            };
        }
    }
}