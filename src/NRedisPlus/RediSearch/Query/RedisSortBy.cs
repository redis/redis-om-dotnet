using System.Collections.Generic;
using NRedisPlus.Model;

namespace NRedisPlus.RediSearch.Query
{
    /// <summary>
    /// a sort-by predicate for a search.
    /// </summary>
    public class RedisSortBy : QueryOption
    {
        /// <summary>
        /// gets or sets the field to sort by.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the direction to sort by.
        /// </summary>
        public SortDirection Direction { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> QueryText
        {
            get
            {
                var dir = Direction == SortDirection.Ascending ? "ASC" : "DESC";
                return new[]
                {
                    "SORTBY",
                    Field,
                    dir,
                };
            }
        }
    }
}
