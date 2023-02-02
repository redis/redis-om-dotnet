using System;
using System.Collections.Generic;
using System.Text;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// An suggestion to use to decorate class level objects you wish to store in redis.
    /// </summary>
    public class Suggestion
    {
        /// <summary>
        /// Gets or sets a value indicating whether set the query to return a factored score for each results. This is useful to merge results from multiple queries.
        /// </summary>
        public string PerfixString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether set the query to return a factored score for each results. This is useful to merge results from multiple queries.
        /// </summary>
        public bool Fuzzy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether set the query to return object payloads, if any were given.
        /// </summary>
        public int Max { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether set the query to return a factored score for each results. This is useful to merge results from multiple queries.
        /// </summary>
        public bool WithScores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether set the query to return object payloads, if any were given.
        /// </summary>
        public bool WithPayloads { get; set; }

        /// <summary>
        /// An suggestion to use to decorate class level objects you wish to store in redis.
        /// </summary>
        /// <returns>the query itself.</returns>
        public static Suggestion Get()
        {
            return new Suggestion();
        }

        /// <summary>
        /// Set the query to return a factored score for each results. This is useful to merge results from
        /// multiple queries.
        /// </summary>
        /// <returns>the query itself.</returns>
        public Suggestion SetWithScores()
        {
            WithScores = true;
            return this;
        }

        /// <summary>
        /// Set the query to return object payloads, if any were given.
        /// </summary>
        /// <returns>the query itself.</returns>
        public Suggestion SetWithPayload()
        {
            WithPayloads = true;
            return this;
        }

        /// <summary>
        /// Set the query to return object fuzzy, if any were given.
        /// </summary>
        /// <returns>the query itself.</returns>
        public Suggestion SetFuzzy()
        {
            Fuzzy = true;
            return this;
        }

        /// <summary>
        /// Set the query to return object fuzzy, if any were given.
        /// </summary>
        /// <param name="value">max limit value for suggestion default 5.</param>
        /// <returns>the query itself.</returns>
        public Suggestion SetMax(int value = 5)
        {
            Max = value;
            return this;
        }

        /// <summary>
        /// Serialize .
        /// </summary>
        /// <param name="args">string value for suggestion.</param>
        /// <returns>An array of strings (the serialized args for redis).</returns>
        internal string[] SerializeGetSuggestions(List<string> args)
        {
            if (Fuzzy)
            {
                args.Add("FUZZY");
            }

            if (Max > 0)
            {
                args.Add("MAX");
                args.Add(Max.ToString());
            }

            if (WithScores)
            {
                args.Add("WITHSCORES");
            }

            if (WithPayloads)
            {
                args.Add("WITHPAYLOADS");
            }

            return args.ToArray();
        }
    }
}
