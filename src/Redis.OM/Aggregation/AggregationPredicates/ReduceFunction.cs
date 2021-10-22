namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// the name of a reduction function.
    /// </summary>
    public enum ReduceFunction
    {
        /// <summary>
        /// count.
        /// </summary>
        COUNT = 0,

        /// <summary>
        /// distinct count.
        /// </summary>
        COUNT_DISTINCT = 1,

        /// <summary>
        /// An approximate count of distinct occurrences of a field.
        /// </summary>
        COUNT_DISTINCTISH = 2,

        /// <summary>
        /// The sum.
        /// </summary>
        SUM = 3,

        /// <summary>
        /// Min.
        /// </summary>
        MIN = 4,

        /// <summary>
        /// Max.
        /// </summary>
        MAX = 5,

        /// <summary>
        /// Average.
        /// </summary>
        AVG = 6,

        /// <summary>
        /// Standard deviation
        /// </summary>
        STDDEV = 7,

        /// <summary>
        /// Quantile.
        /// </summary>
        QUANTILE = 8,

        /// <summary>
        /// Sends distinct elements to a list.
        /// </summary>
        TOLIST = 9,

        /// <summary>
        /// retrieves the first value matching the pattern.
        /// </summary>
        FIRST_VALUE = 10,

        /// <summary>
        /// Gets a random sample.
        /// </summary>
        RANDOM_SAMPLE = 11,
    }
}
