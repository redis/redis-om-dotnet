namespace Redis.OM.Searching.Query
{
    /// <summary>
    /// Components of a KNN search.
    /// </summary>
    public class NearestNeighbors
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NearestNeighbors"/> class.
        /// </summary>
        /// <param name="propertyName">The property name to search on.</param>
        /// <param name="numNeighbors">The number of nearest neighbors.</param>
        /// <param name="vectorBlob">The vector blob.</param>
        public NearestNeighbors(string propertyName, int numNeighbors, byte[] vectorBlob)
        {
            PropertyName = propertyName;
            NumNeighbors = numNeighbors;
            VectorBlob = vectorBlob;
        }

        /// <summary>
        /// Gets the name of the property to perform the vector search on.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the number of neighbors to find.
        /// </summary>
        public int NumNeighbors { get; }

        /// <summary>
        /// Gets the Vector blob to search on.
        /// </summary>
        public byte[] VectorBlob { get; }
    }
}