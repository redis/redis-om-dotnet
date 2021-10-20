using Redis.OM.Schema;

namespace Redis.OM.RediSearch.Attributes
{
    /// <summary>
    /// Decorate a numeric string or geo field to add an index to it.
    /// </summary>
    public sealed class IndexedAttribute : SearchFieldAttribute
    {
        /// <summary>
        /// gets or sets the separator to use for string fields. defaults to. <code>|</code>.
        /// </summary>
        public char Separator { get; set; } = '|';

        /// <summary>
        /// Gets or sets a value indicating whether text is case sensitive.
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <inheritdoc/>
        internal override SearchFieldType SearchFieldType => SearchFieldType.INDEXED;
    }
}
