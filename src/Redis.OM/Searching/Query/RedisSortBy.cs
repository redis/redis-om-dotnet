using System.Collections.Generic;
using Redis.OM.Model;

namespace Redis.OM.Searching.Query
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
        internal override IEnumerable<string> SerializeArgs
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
