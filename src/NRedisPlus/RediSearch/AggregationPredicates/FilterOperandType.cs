namespace NRedisPlus.RediSearch.AggregationPredicates
{
    /// <summary>
    /// The type of operand you are looking at when parsing expressions.
    /// </summary>
    internal enum FilterOperandType
    {
        /// <summary>
        /// The item is an identifier.
        /// </summary>
        Identifier = 0,

        /// <summary>
        /// The item is a literal numeric.
        /// </summary>
        Numeric = 1,

        /// <summary>
        /// The item is a string literal.
        /// </summary>
        String = 2,
    }
}
