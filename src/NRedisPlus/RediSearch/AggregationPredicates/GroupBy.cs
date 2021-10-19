using System.Collections.Generic;
using System.Linq;

namespace NRedisPlus.RediSearch.AggregationPredicates
{
    /// <summary>
    /// A predicate indicating that you want to group like objects together.
    /// </summary>
    public class GroupBy : IAggregationPredicate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupBy"/> class.
        /// </summary>
        /// <param name="properties">the properties to group.</param>
        public GroupBy(string[] properties)
        {
            Properties = properties;
        }

        /// <summary>
        /// Gets or sets the properties to group.
        /// </summary>
        public string[] Properties { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> Serialize()
        {
            var ret = new List<string>
            {
                "GROUPBY",
                Properties.Length.ToString(),
            };
            ret.AddRange(Properties.Select(property => $"@{property}"));
            return ret.ToArray();
        }
    }
}
