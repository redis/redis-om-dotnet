using System.Collections.Generic;

namespace Redis.OM.Searching.Query
{
    /// <summary>
    /// Limits the search results.
    /// </summary>
    public class SearchLimit : QueryOption
    {
        /// <summary>
        /// Gets or sets the offset into the result to start at.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the number of items to return.
        /// </summary>
        public int Number { get; set; } = 10;

        /// <inheritdoc/>
        internal override IEnumerable<string> SerializeArgs
        {
            get
            {
                return new[]
                {
                    "LIMIT",
                    Offset.ToString(),
                    Number.ToString(),
                };
            }
        }
    }
}
