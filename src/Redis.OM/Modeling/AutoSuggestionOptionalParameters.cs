namespace Redis.OM.Modeling
{
    /// <summary>
    /// Determine how the item will be stored in Redis.
    /// </summary>
    public enum AutoSuggestionOptionalParameters
    {
        /// <summary>
        /// Increments the existing entry of the suggestion by the given score .
        /// </summary>
        INCR = 0,

        /// <summary>
        /// Performs a fuzzy prefix search, including prefixes at Levenshtein distance of 1 from the prefix sent.
        /// </summary>
        FUZZY = 1,

        /// <summary>
        /// Returns the score of each suggestion.
        /// </summary>
        WITHSCORES = 2,

        /// <summary>
        /// Returns optional payloads saved along with the suggestions.
        /// </summary>
        WITHPAYLOADS = 3,
    }
}
