namespace Redis.OM
{
    /// <summary>
    /// Represents a vector result with its score and the document associated with it.
    /// </summary>
    /// <typeparam name="T">the document type.</typeparam>
    public class VectorResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VectorResult{T}"/> class.
        /// </summary>
        /// <param name="document">the document.</param>
        /// <param name="score">the score.</param>
        internal VectorResult(T document, double score)
        {
            Score = score;
            Document = document;
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        public T Document { get; }

        /// <summary>
        /// Gets the score.
        /// </summary>
        public double Score { get; }
    }
}