using System;
using System.Collections.Generic;
using System.Linq;

namespace NRedisPlus.RediSearch
{
    internal class HashDiff : IObjectDiff
    {
        private readonly IEnumerable<KeyValuePair<string, string>> _setFieldValuePairs;
        private readonly IEnumerable<string> _delValues;

        public HashDiff(IEnumerable<KeyValuePair<string, string>> setSetFieldValuePairs, IEnumerable<string> delValues)
        {
            _setFieldValuePairs = setSetFieldValuePairs;
            _delValues = delValues;
        }
        public string[] SerializeScriptArgs()
        {
            var ret = new List<string>();
            ret.Add(_setFieldValuePairs.Count().ToString());
            foreach (var kvp in _setFieldValuePairs)
            {
                ret.Add(kvp.Key);
                ret.Add(kvp.Value);
            }
            ret.AddRange(_delValues);
            return ret.ToArray();
        }

        public string Script => nameof(Scripts.HASH_DIFF_RESOLUTION);
    }
}