using Redis.OM.Modeling;

namespace Redis.OM.Modeling
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

        /// <summary>
        /// Gets or sets the vector storage algorithm to use. Defaults to Flat (which is brute force).
        /// </summary>
        public VectorAlgorithm Algorithm { get; set; } = VectorAlgorithm.FLAT;

        /// <summary>
        /// Gets or sets the Supported distance metric.
        /// </summary>
        public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.L2;

        /// <summary>
        /// Gets or sets the Initial vector capacity in the index affecting memory allocation size of the index.
        /// </summary>
        public int InitialCapacity { get; set; }

        /// <summary>
        /// Gets or sets Block size to hold BLOCK_SIZE amount of vectors in a contiguous array. This is useful when the
        /// index is dynamic with respect to addition and deletion. Defaults to 1024.
        /// </summary>
        public int BlockSize { get; set; }

        /// <summary>
        /// gets or sets the number of maximum allowed outgoing edges for each node in the graph in each layer.
        /// On layer zero the maximal number of outgoing edges will be 2M. Default is 16.
        /// </summary>
        public int M { get; set; }

        /// <summary>
        /// Gets or sets the number of maximum allowed potential outgoing edges candidates for each node in the graph,
        /// during the graph building. Default is 200.
        /// </summary>
        public int EfConstructor { get; set; }

        /// <summary>
        /// Gets or sets the number of maximum top candidates to hold during the KNN search. Higher values of
        /// EfRuntime lead to more accurate results at the expense of a longer runtime. Default is 10.
        /// </summary>
        public int EfRuntime { get; set; }

        /// <summary>
        /// Gets or sets Relative factor that sets the boundaries in which a range query may search for candidates.
        /// That is, vector candidates whose distance from the query vector is radius*(1 + EPSILON) are potentially
        /// scanned, allowing more extensive search and more accurate results (on the expense of runtime). Default is 0.01.
        /// </summary>
        public double Epsilon { get; set; }

        /// <inheritdoc/>
        internal override SearchFieldType SearchFieldType => SearchFieldType.INDEXED;

        /// <summary>
        /// gets the number of arguments that will be produced by this attribute.
        /// </summary>
        internal int NumArgs
        {
            get
            {
                var numArgs = 6;
                numArgs += InitialCapacity != default ? 2 : 0;
                if (Algorithm == VectorAlgorithm.FLAT)
                {
                    numArgs += BlockSize != default ? 2 : 0;
                }

                if (Algorithm == VectorAlgorithm.HNSW)
                {
                    numArgs += M != default ? 2 : 0;
                    numArgs += EfConstructor != default ? 2 : 0;
                    numArgs += EfRuntime != default ? 2 : 0;
                    numArgs += Epsilon != default ? 2 : 0;
                }

                return numArgs;
            }
        }
    }
}
