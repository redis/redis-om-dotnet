using System.Collections.Generic;

namespace NRedisPlus.RediSearch.Query
{
    /// <summary>
    /// An option within a query.
    /// </summary>
    public abstract class QueryOption
    {
        /// <summary>
        /// Gets a serialized array of strings for a query.
        /// </summary>
        internal abstract IEnumerable<string> SerializeArgs { get; }
    }
}
