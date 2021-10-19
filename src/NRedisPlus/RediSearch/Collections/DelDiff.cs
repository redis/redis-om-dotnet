using System;

namespace NRedisPlus.RediSearch.Collections
{
    /// <summary>
    /// A diff that will delete the property.
    /// </summary>
    public class DelDiff : IObjectDiff
    {
        /// <inheritdoc/>
        public string Script => nameof(Scripts.Unlink);

        /// <inheritdoc/>
        public string[] SerializeScriptArgs()
        {
            return Array.Empty<string>();
        }
    }
}
