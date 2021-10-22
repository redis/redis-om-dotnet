using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// A diff that will delete the property.
    /// </summary>
    internal class DelDiff : IObjectDiff
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
