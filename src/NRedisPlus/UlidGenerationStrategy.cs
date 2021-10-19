using System;

namespace NRedisPlus
{
    /// <summary>
    /// generation strategy that generates a <see href="https://github.com/ulid/spec">ULID</see>.
    /// </summary>
    public class UlidGenerationStrategy : IIdGenerationStrategy
    {
        /// <summary>
        /// Generates a <see href="https://github.com/ulid/spec">ULID</see>.
        /// </summary>
        /// <returns>A Ulid.</returns>
        public string GenerateId()
        {
            return Ulid.NewUlid().ToString();
        }
    }
}
