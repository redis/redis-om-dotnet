using System;
using Redis.OM.Model;

namespace Redis.OM.Schema
{
    /// <summary>
    /// An attribute to use to decorate class level objects you wish to store in redis.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DocumentAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the storage type.
        /// </summary>
        public StorageType StorageType { get; set; } = StorageType.Hash;

        /// <summary>
        /// Gets or sets the IdGenerationStrategy, will use a ULID by default.
        /// </summary>
        public IIdGenerationStrategy IdGenerationStrategy { get; set; } = new UlidGenerationStrategy();

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
    }
}
