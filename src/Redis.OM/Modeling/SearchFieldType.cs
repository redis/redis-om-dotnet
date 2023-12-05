namespace Redis.OM.Modeling
{
    /// <summary>
    /// The search field type.
    /// </summary>
    internal enum SearchFieldType
    {
        /// <summary>
        /// A text index field.
        /// </summary>
        TEXT = 0,

        /// <summary>
        /// A numeric index field.
        /// </summary>
        NUMERIC = 1,

        /// <summary>
        /// A geo index field.
        /// </summary>
        GEO = 2,

        /// <summary>
        /// A tag index field.
        /// </summary>
        TAG = 3,

        /// <summary>
        /// A generically indexed field - the library will figure out how to index.
        /// </summary>
        INDEXED = 4,

        /// <summary>
        /// A vector index field.
        /// </summary>
        VECTOR = 5,
    }
}
