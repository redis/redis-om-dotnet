using System;
using System.Collections.Generic;
using System.Linq;

namespace Redis.OM.Searching
{
    /// <summary>
    /// The result from a search.
    /// </summary>
    public class SearchResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="val">The redis response.</param>
        public SearchResponse(RedisReply val)
        {
            var vals = NormalizeReply(val);
            DocumentCount = vals[0];
            Documents = new Dictionary<string, IDictionary<string, string>>();
            Scores = new Dictionary<string, double>();
            for (var i = 1; i < vals.Count();)
            {
                var docId = (string)vals[i];
                var documentIndex = GetDocumentIndex(vals, i + 1);
                if (documentIndex >= vals.Length)
                {
                    break;
                }

                if (documentIndex > i + 1 && TryGetScore(vals[i + 1], out var score))
                {
                    Scores[docId] = score;
                }

                var documentHash = new Dictionary<string, string>();
                var docArray = vals[documentIndex].ToArray();
                for (var j = 0; j < docArray.Length; j += 2)
                {
                    documentHash.Add(docArray[j], docArray[j + 1]);
                }

                Documents.Add(docId, documentHash);
                i = documentIndex + 1;
            }
        }

        /// <summary>
        /// Gets the number of documents found by the search.
        /// </summary>
        public long DocumentCount { get; }

        /// <summary>
        /// Gets the documents from the search.
        /// </summary>
        public IDictionary<string, IDictionary<string, string>> Documents { get; }

        /// <summary>
        /// Gets the relative internal scores of each document keyed by document id. Only populated when the
        /// query was run with the <see cref="Query.QueryFlags.WithScores"/> flag.
        /// </summary>
        public IDictionary<string, double> Scores { get; }

        /// <summary>
        /// gets document as a collection of the provided type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>A dictionary of the response type with their keys.</returns>
        public IDictionary<string, T> DocumentsAs<T>()
            where T : notnull
        {
            var dict = new Dictionary<string, T>();
            foreach (var kvp in Documents)
            {
                var rrDict = kvp.Value.ToDictionary(x => x.Key, x => (RedisReply)x.Value);
                var obj = RedisObjectHandler.FromHashSet<T>(rrDict);
                dict.Add(kvp.Key, obj);
            }

            return dict;
        }

        /// <summary>
        /// Normalizes a search reply into the flat RESP2 layout the parsers expect. RESP2 replies are
        /// returned unchanged; a RESP3 map reply (negotiated automatically by newer StackExchange.Redis
        /// versions) is reshaped from its <c>total_results</c>/<c>results</c> structure into the legacy
        /// <c>[count, id, (score,) fields, ...]</c> array.
        /// </summary>
        /// <param name="val">The raw search reply.</param>
        /// <returns>The reply in flat RESP2 layout.</returns>
        internal static RedisReply[] NormalizeReply(RedisReply val)
        {
            if (!val.IsMap)
            {
                return val.ToArray();
            }

            var flattened = new List<RedisReply> { val.GetMapValueOrDefault("total_results") ?? 0L };
            var results = val.GetMapValueOrDefault("results");
            if (results is not null)
            {
                foreach (var result in results.ToArray())
                {
                    flattened.Add(result.GetMapValueOrDefault("id") ?? string.Empty);

                    // WITHSCORES surfaces the score as a scalar map entry; the existing parser treats it
                    // as the metadata sitting between the id and the field payload.
                    var score = result.GetMapValueOrDefault("score");
                    if (score is not null)
                    {
                        flattened.Add(score);
                    }

                    flattened.Add(result.GetMapValueOrDefault("extra_attributes") ?? new RedisReply(Array.Empty<RedisReply>()));
                }
            }

            return flattened.ToArray();
        }

        /// <summary>
        /// Walks forward over scalar metadata entries (e.g. the score emitted by WITHSCORES) that sit between a
        /// document's id and its field payload, returning the index of the field payload.
        /// </summary>
        /// <param name="values">The flat search reply.</param>
        /// <param name="startIndex">The index immediately after the document id.</param>
        /// <returns>The index of the document's field payload.</returns>
        internal static int GetDocumentIndex(RedisReply[] values, int startIndex)
        {
            while (startIndex < values.Length && values[startIndex].ToArray().Length == 1)
            {
                startIndex++;
            }

            return startIndex;
        }

        /// <summary>
        /// Attempts to interpret a scalar metadata reply as a numeric document score.
        /// </summary>
        /// <param name="reply">The scalar reply sitting between the document id and its payload.</param>
        /// <param name="score">The parsed score.</param>
        /// <returns>Whether the reply could be interpreted as a numeric score.</returns>
        internal static bool TryGetScore(RedisReply reply, out double score)
        {
            try
            {
                score = (double)reply;
                return true;
            }
            catch (InvalidCastException)
            {
                score = default;
                return false;
            }
        }
    }

    /// <summary>
    /// A strongly typed search response.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
#pragma warning disable SA1402

    public class SearchResponse<T>
#pragma warning restore SA1402
        where T : notnull
    {
        private const string TimeoutText = "Timeout limit was reached";

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResponse{T}"/> class.
        /// </summary>
        /// <param name="val">The response to use to initialize the Search Response.</param>
        public SearchResponse(RedisReply val)
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (type.IsPrimitive || type == typeof(string))
            {
                var @this = PrimitiveSearchResponse(val);
                Documents = @this.Documents;
                DocumentCount = @this.DocumentCount;
                Scores = @this.Scores;
            }
            else if (underlyingType is { IsPrimitive: true })
            {
                var @this = PrimitiveSearchResponse(val);
                Documents = @this.Documents;
                DocumentCount = @this.DocumentCount;
                Scores = @this.Scores;
            }
            else
            {
                var vals = SearchResponse.NormalizeReply(val);
                if (vals.Length == 1)
                {
                    var str = vals[0].ToString();
                    if (str == TimeoutText)
                    {
                        throw new TimeoutException(
                            "Encountered timeout when searching - check the duration of your query.");
                    }
                }

                DocumentCount = vals[0];
                Documents = new Dictionary<string, T>();
                Scores = new Dictionary<string, double>();
                for (var i = 1; i < vals.Count();)
                {
                    var docId = (string)vals[i];
                    var documentIndex = SearchResponse.GetDocumentIndex(vals, i + 1);
                    if (documentIndex >= vals.Length)
                    {
                        break;
                    }

                    if (documentIndex > i + 1 && SearchResponse.TryGetScore(vals[i + 1], out var score))
                    {
                        Scores[docId] = score;
                    }

                    var documentHash = new Dictionary<string, RedisReply>();
                    var docArray = vals[documentIndex].ToArray();
                    if (docArray.Length > 1)
                    {
                        for (var j = 0; j < docArray.Length; j += 2)
                        {
                            documentHash.Add(docArray[j], docArray[j + 1]);
                        }

                        var obj = RedisObjectHandler.FromHashSet<T>(documentHash);
                        Documents.Add(docId, obj);
                    }
                    else
                    {
                        DocumentsSkippedCount++; // needed when a key expired while it was being enumerated by Redis.
                    }

                    i = documentIndex + 1;
                }
            }
        }

        private SearchResponse()
        {
            DocumentCount = 0;
            DocumentsSkippedCount = 0;
            Documents = new Dictionary<string, T>();
            Scores = new Dictionary<string, double>();
        }

        /// <summary>
        /// Gets or sets the number of documents found by the search.
        /// </summary>
        public long DocumentCount { get; set; }

        /// <summary>
        /// Gets the number of documents skipped while enumerating the search result set.
        /// This can be indicative of documents that have expired during enumeration.
        /// </summary>
        public int DocumentsSkippedCount { get; private set; }

        /// <summary>
        /// Gets the documents.
        /// </summary>
        public IDictionary<string, T> Documents { get; }

        /// <summary>
        /// Gets the relative internal scores of each document keyed by document id. Only populated when the
        /// query was run with the <see cref="Query.QueryFlags.WithScores"/> flag.
        /// </summary>
        public IDictionary<string, double> Scores { get; }

        /// <summary>
        /// Gets a particular document by it's ID.
        /// </summary>
        /// <param name="key">the key to use to look up.</param>
        public T this[string key] => Documents[key];

        /// <summary>
        /// Gets a particular element by its index in the collection.
        /// </summary>
        /// <param name="index">the index.</param>
        internal T this[int index] => Documents.Values.ElementAt(index);

        private static SearchResponse<T> PrimitiveSearchResponse(RedisReply redisReply)
        {
            var arr = SearchResponse.NormalizeReply(redisReply);
            var response = new SearchResponse<T>();
            response.DocumentCount = arr[0];
            for (var i = 1; i < arr.Count();)
            {
                var docId = (string)arr[i];
                var documentIndex = SearchResponse.GetDocumentIndex(arr, i + 1);
                if (documentIndex >= arr.Length)
                {
                    break;
                }

                if (documentIndex > i + 1 && SearchResponse.TryGetScore(arr[i + 1], out var score))
                {
                    response.Scores[docId] = score;
                }

                T? primitive = arr[documentIndex].ToArray().Length > 1 ? (T)Convert.ChangeType(arr[documentIndex].ToArray()[1], typeof(T)) : default;
                response.Documents.Add(docId, primitive!);
                i = documentIndex + 1;
            }

            return response;
        }
    }
}
