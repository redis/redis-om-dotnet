using System;
using Redis.OM.Modeling;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Marks a field as searchable within a Redis Document.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class SearchableAttribute : SearchFieldAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether whether or not to index with stemming.
        /// </summary>
        public bool NoStem { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicting which phonetic matcher to use.
        /// </summary>
        public string PhoneticMatcher { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the weight of a given field.
        /// </summary>
        public double Weight { get; set; } = 1;

        /// <inheritdoc/>
        internal override SearchFieldType SearchFieldType => SearchFieldType.TEXT;
    }
}
