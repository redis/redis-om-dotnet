using System;
using Redis.OM.Schema;

namespace Redis.OM.RediSearch.Attributes
{
    /// <summary>
    /// Decorates a field that you want to index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class SearchFieldAttribute : RedisFieldAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether the field will be sortable.
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field will be aggregatable.
        /// </summary>
        public bool Aggregatable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether text will be normalized when indexed
        /// (sent to lower case with no diacritics). Defaults to true.
        /// </summary>
        public bool Normalize { get; set; } = true;

        /// <summary>
        /// Gets the type of index.
        /// </summary>
        internal abstract SearchFieldType SearchFieldType { get; }
    }
}
