using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Redis.OM;

namespace Redis.OM.Aggregation
{
    /// <summary>
    /// A result of an aggregation.
    /// </summary>
    /// <typeparam name="T">This is the type of the record shell, which should only be used when building
    /// the pipeline.</typeparam>
#pragma warning disable SA1402
    public class AggregationResult<T> : IAggregationResult
#pragma warning restore SA1402
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregationResult{T}"/> class.
        /// </summary>
        /// <param name="res">The redis reply to use when building the aggregation result.</param>
        private AggregationResult(RedisReply res)
        {
            Aggregations = new Dictionary<string, RedisReply>();
            var arr = res.ToArray();
            if (arr.Length == 1 && arr.First().ToString(CultureInfo.InvariantCulture) ==
                "Value was not found in result (not a hard error)")
            {
                throw new NotSupportedException(
                    $"Received the error: {arr.First().ToString(CultureInfo.InvariantCulture)} from Redis. " +
                    $"This implies that one of the fields you attempted to access in you Aggregation was not marked as " +
                    $"Aggregatable or Sortbable at the time of index creation, or was not loaded properly in the pipeline. " +
                    $"Please review your model and aggregation and try again.");
            }

            for (var i = 0; i < arr.Length; i += 2)
            {
                Aggregations.Add(arr[i], arr[i + 1]);
            }
        }

        /// <summary>
        /// Gets a Shell of a Record stored on the database, only appropriate to use
        /// inside of aggregation pipelines, no real data will be stored in here once
        /// the AggregationResult is Hydrated.
        /// </summary>
        public T? RecordShell { get; } = default;

        /// <summary>
        /// Gets the computed aggregations. When materialized, this is a completed set of aggregations.
        /// When building the pipeline, you can use fields computed further up the pipeline.
        /// </summary>
        public IDictionary<string, RedisReply> Aggregations { get; }

        /// <summary>
        /// Accesses an aggregation directly.
        /// </summary>
        /// <param name="key">the aggregation alias.</param>
        public RedisReply this[string key] => Aggregations[key];

        /// <summary>
        /// Hydrates the record from the available properties. Basically, this will place all the properties found in "Aggregations"
        /// into an instance of your model, and in the special case where you've called a LoadAll on a JSON model, this should parse the entire record.
        /// </summary>
        /// <returns>An instance of the base class hydrated as much as possible by the Results of the aggregation.</returns>
        public T Hydrate() => RedisObjectHandler.FromHashSet<T>(Aggregations);

        /// <summary>
        /// Initializes a set of aggregations from an aggregation result.
        /// </summary>
        /// <param name="res">the result to enumerate.</param>
        /// <returns>A set of Aggregation Results.</returns>
        internal static IEnumerable<AggregationResult<T>> FromRedisResult(RedisReply res)
        {
            var arr = res.ToArray();
            for (var i = 1; i < arr.Length; i++)
            {
                yield return new AggregationResult<T>(arr[i]);
            }
        }
    }

    /// <summary>
    /// Non-generic aggregation result.
    /// </summary>
    internal class AggregationResult : IAggregationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregationResult"/> class.
        /// </summary>
        /// <param name="res">the RedisReply to build the result from.</param>
        private AggregationResult(RedisReply res)
        {
            Aggregations = new Dictionary<string, RedisReply>();
            var arr = res.ToArray();
            for (var i = 0; i < arr.Length; i += 2)
            {
                Aggregations.Add(arr[i], arr[i + 1]);
            }
        }

        /// <summary>
        /// Gets the aggregations.
        /// </summary>
        public IDictionary<string, RedisReply> Aggregations { get; }

        /// <summary>
        /// Indexes directly to a particular aggregation.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        public RedisReply this[string key] => Aggregations[key];

        /// <summary>
        /// Initialize an enumerable of Aggregation Results from a Redis Reply.
        /// </summary>
        /// <param name="res">the reply to initialize the results from.</param>
        /// <returns>an enumerable of results.</returns>
        public static IEnumerable<AggregationResult> FromRedisResult(RedisReply res)
        {
            var arr = res.ToArray();
            for (var i = 1; i < arr.Length; i++)
            {
                yield return new AggregationResult(arr[i]);
            }
        }
    }
}
