namespace Redis.OM.Modeling
{
    /// <summary>
    /// A result from a vector search.
    /// </summary>
    /// <typeparam name="T">The Document type.</typeparam>
    public class VectorResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VectorResult{T}"/> class.
        /// </summary>
        /// <param name="score">the score.</param>
        /// <param name="document">the document.</param>
        internal VectorResult(double score, T document)
        {
            Score = score;
            Document = document;
        }

        /// <summary>
        /// Gets the distance score between this document and the queried vector.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the document part of the result.
        /// </summary>
        public T Document { get; }
    }
}