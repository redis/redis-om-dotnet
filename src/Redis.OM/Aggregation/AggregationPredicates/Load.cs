using System.Collections.Generic;
using System.Linq;

namespace Redis.OM.Aggregation.AggregationPredicates
{
    /// <summary>
    /// Represents a Load aggregation predicate.
    /// </summary>
    public class Load : IAggregationPredicate
    {
        private const string LoadString = "LOAD";
        private IEnumerable<string> _properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="Load"/> class.
        /// </summary>
        /// <param name="properties">The properties to load.</param>
        public Load(IEnumerable<string> properties)
        {
            _properties = properties;
        }

        /// <inheritdoc />
        public IEnumerable<string> Serialize()
        {
            yield return LoadString;
            yield return _properties.Count().ToString();
            foreach (var property in _properties)
            {
                yield return property;
            }
        }
    }
}