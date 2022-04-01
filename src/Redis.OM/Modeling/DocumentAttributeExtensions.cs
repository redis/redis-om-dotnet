using System;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// utility functions for document attribute class.
    /// </summary>
    internal static class DocumentAttributeExtensions
    {
        /// <summary>
        /// Get's the index name for the attribute and type.
        /// </summary>
        /// <param name="attr">The document attribute.</param>
        /// <param name="type">The type.</param>
        /// <returns>The index name.</returns>
        internal static string GetIndexName(this DocumentAttribute attr, Type type) => string.IsNullOrEmpty(attr.IndexName) ? $"{type.Name.ToLower()}-idx" : attr.IndexName!;
    }
}