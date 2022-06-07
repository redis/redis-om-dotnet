using System.Collections.Generic;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// Represents an aggregation predicate to load all properties of a model.
    /// </summary>
    public class LoadAll : IAggregationPredicate
    {
        private const string LoadString = "LOAD";
        private const string Star = "*";

        /// <inheritdoc />
        public IEnumerable<string> Serialize()
        {
            yield return LoadString;
            yield return Star;
        }
    }
}