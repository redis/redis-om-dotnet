using System;
using System.Collections.Generic;
using Redis.OM;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// An attribute to use to decorate class level objects you wish to store in redis.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DocumentAttribute : Attribute
    {
        private static Dictionary<string, IIdGenerationStrategy> _idGenerationStrategies = new ()
        {
            { nameof(UlidGenerationStrategy), new UlidGenerationStrategy() },
            { nameof(Uuid4IdGenerationStrategy), new Uuid4IdGenerationStrategy() },
        };

        /// <summary>
        /// Gets or sets the storage type.
        /// </summary>
        public StorageType StorageType { get; set; } = StorageType.Hash;

        /// <summary>
        /// Gets or sets the IdGenerationStrategy, will use a ULID by default.
        /// </summary>
        public string IdGenerationStrategyName { get; set; } = nameof(UlidGenerationStrategy);

        /// <summary>
        /// Gets or sets the prefixes to use for the Documents.
        /// </summary>
        public string[] Prefixes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the name of the index, will default to className-idx.
        /// </summary>
        public string? IndexName { get; set; }

        /// <summary>
        /// Gets or sets The language the documents are stored in.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the field in the document to check for the language in.
        /// </summary>
        public string? LanguageField { get; set; }

        /// <summary>
        /// Gets or sets the filter to use for indexing documents.
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Gets or sets The stopwords to use for this index. If not set, Redis will use the
        /// <see href="https://redis.io/docs/stack/search/reference/stopwords/#default-stop-word-list">default</see> stopwords for this index.
        /// </summary>
        public string[]? Stopwords { get; set; }

        /// <summary>
        /// Gets the IdGenerationStrategy.
        /// </summary>
        internal IIdGenerationStrategy IdGenerationStrategy => _idGenerationStrategies[IdGenerationStrategyName];

        /// <summary>
        /// Registers an Id generation Strategy with the Object Mapper.
        /// </summary>
        /// <param name="strategyName">The name of the strategy, which you will reference when declaring a Document.</param>
        /// <param name="strategy">An instance of the Strategy class to be used by all documents to generate an id.</param>
        public static void RegisterIdGenerationStrategy(string strategyName, IIdGenerationStrategy strategy)
        {
            _idGenerationStrategies.Add(strategyName, strategy);
        }
    }
}
