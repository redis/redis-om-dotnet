using System.Collections.Generic;

namespace NRedisPlus.RediSearch.Query
{
    /// <summary>
    /// Filter to use when querying.
    /// </summary>
    public class RedisFilter : QueryOption
    {
        /// <summary>
        /// Gets or sets field filter on.
        /// </summary>
        private readonly string _fieldName;

        /// <summary>
        /// Gets or sets the min.
        /// </summary>
        private readonly int _min;

        /// <summary>
        /// Gets or sets the max.
        /// </summary>
        private readonly int _max;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisFilter"/> class.
        /// </summary>
        /// <param name="fieldName">the field name.</param>
        /// <param name="min">the min value.</param>
        /// <param name="max">the max value.</param>
        public RedisFilter(string fieldName, int min = int.MinValue, int max = int.MaxValue)
        {
            _fieldName = fieldName;
            _min = min;
            _max = max;
        }

        /// <inheritdoc/>
        internal override IEnumerable<string> SerializeArgs
        {
            get
            {
                var ret = new List<string>();
                ret.Add("FILTER");
                ret.Add(_fieldName);
                ret.Add(_min.ToString());
                ret.Add(_max.ToString());
                return ret.ToArray();
            }
        }
    }
}
