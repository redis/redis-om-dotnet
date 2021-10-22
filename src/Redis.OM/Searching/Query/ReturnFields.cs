using System.Collections.Generic;
using System.Linq;

namespace Redis.OM.Searching.Query
{
    /// <summary>
    /// Predicate denoting the fields that will be returned from redis.
    /// </summary>
    public class ReturnFields : QueryOption
    {
        /// <summary>
        /// The fields to bring back.
        /// </summary>
        private readonly IEnumerable<string> _fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnFields"/> class.
        /// </summary>
        /// <param name="fields">the fields to return.</param>
        public ReturnFields(IEnumerable<string> fields)
        {
            _fields = fields;
        }

        /// <inheritdoc/>
        internal override IEnumerable<string> SerializeArgs
        {
            get
            {
                var ret = new List<string> { "RETURN", _fields.Count().ToString() };
                foreach (var field in _fields)
                {
                    ret.Add($"{field}");
                }

                return ret.ToArray();
            }
        }
    }
}
