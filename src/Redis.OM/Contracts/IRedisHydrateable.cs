using System.Collections.Generic;

namespace Redis.OM.Contracts
{
    /// <summary>
    /// An object that can be hydrated too and from a Redis Hash.
    /// </summary>
    public interface IRedisHydrateable
    {
        /// <summary>
        /// Hydrates the object.
        /// </summary>
        /// <param name="dict">The dictionary to hydrate from.</param>
        void Hydrate(IDictionary<string, string> dict);

        /// <summary>
        /// Converts object to dictionary for Redis.
        /// </summary>
        /// <returns>A dictionary for Redis.</returns>
        IDictionary<string, object> BuildHashSet();
    }
}
