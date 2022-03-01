using System;
using Redis.OM.Modeling;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Decorates a field that you want to index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
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
        /// Gets or sets the JSON path to the desired attribute to index. This is used for
        /// indexing individual fields within objects. Defaults to ".", which assumes the entire field will be indexed
        /// If the indexed field is a scalar, it will only index that field. If that index is an object the index
        /// will be recursively built based off the <see cref="CascadeDepth"/>.
        /// </summary>
        public string? JsonPath { get; set; }

        /// <summary>
        /// Gets or sets the depth into the object graph to automatically generate the index.
        /// </summary>
        public int CascadeDepth { get; set; }

        /// <summary>
        /// Gets the type of index.
        /// </summary>
        internal abstract SearchFieldType SearchFieldType { get; }
    }
}
