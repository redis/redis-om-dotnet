namespace NRedisPlus.RediSearch.Query
{
    /// <summary>
    /// Flags to indicate the options of a query.
    /// </summary>
    public enum QueryFlags
    {
        /// <summary>
        /// If it appears after the query, we only return the document ids and not the content.
        /// This is useful if RediSearch is only an index on an external document collection.
        /// </summary>
        Nocontent = 1,

        /// <summary>
        /// if set, we do not try to use stemming for query expansion but search the query terms verbatim.
        /// </summary>
        Verbatim = 2,

        /// <summary>
        /// If set, we do not filter stop words from the query.
        /// </summary>
        NoStopWords = 4,

        /// <summary>
        /// If set, we also return the relative internal score of each document. T
        /// his can be used to merge results from multiple instances.
        /// </summary>
        WithScores = 8,

        /// <summary>
        /// If set, we retrieve optional document payloads (see FT.ADD). the payloads follow the document id,
        /// and if WITHSCORES was set, follow the scores.
        /// </summary>
        WithPayloads = 16,

        /// <summary>
        /// Only relevant in conjunction with SORTBY . Returns the value of the sorting key, right after the id and
        /// score and /or payload if requested. This is usually not needed by users,
        /// and exists for distributed search coordination purposes.
        /// </summary>
        WithSortKeys = 32,
    }
}
