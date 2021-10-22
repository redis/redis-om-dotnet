using System.Collections.Generic;
using System.Linq;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// A diff for a hash type object.
    /// </summary>
    internal class HashDiff : IObjectDiff
    {
        private readonly IEnumerable<KeyValuePair<string, string>> _setFieldValuePairs;
        private readonly IEnumerable<string> _delValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashDiff"/> class.
        /// </summary>
        /// <param name="setSetFieldValuePairs">values to set.</param>
        /// <param name="delValues">values to delete.</param>
        public HashDiff(IEnumerable<KeyValuePair<string, string>> setSetFieldValuePairs, IEnumerable<string> delValues)
        {
            _setFieldValuePairs = setSetFieldValuePairs;
            _delValues = delValues;
        }

        /// <inheritdoc/>
        public string Script => nameof(Scripts.HashDiffResolution);

        /// <inheritdoc/>
        public string[] SerializeScriptArgs()
        {
            var ret = new List<string> { _setFieldValuePairs.Count().ToString() };
            foreach (var kvp in _setFieldValuePairs)
            {
                ret.Add(kvp.Key);
                ret.Add(kvp.Value);
            }

            ret.AddRange(_delValues);
            return ret.ToArray();
        }
    }
}
