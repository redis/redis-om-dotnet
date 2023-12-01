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
            var vals = val.ToArray();
            DocumentCount = vals[0];
            Documents = new Dictionary<string, IDictionary<string, string>>();
            for (var i = 1; i < vals.Count(); i += 2)
            {
                var docId = (string)vals[i];
                var documentHash = new Dictionary<string, string>();
                var docArray = vals[i + 1].ToArray();
                for (var j = 0; j < docArray.Length; j += 2)
                {
                    documentHash.Add(docArray[j], docArray[j + 1]);
                }

                Documents.Add(docId, documentHash);
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
            }
            else if (underlyingType is { IsPrimitive: true })
            {
                var @this = PrimitiveSearchResponse(val);
                Documents = @this.Documents;
                DocumentCount = @this.DocumentCount;
            }
            else
            {
                var vals = val.ToArray();
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
                for (var i = 1; i < vals.Count(); i += 2)
                {
                    var docId = (string)vals[i];
                    var documentHash = new Dictionary<string, RedisReply>();
                    var docArray = vals[i + 1].ToArray();
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
                }
            }
        }

        private SearchResponse()
        {
            DocumentCount = 0;
            DocumentsSkippedCount = 0;
            Documents = new Dictionary<string, T>();
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
            var arr = redisReply.ToArray();
            var response = new SearchResponse<T>();
            response.DocumentCount = arr[0];
            for (var i = 1; i < arr.Count(); i += 2)
            {
                var docId = (string)arr[i];
                T? primitive = arr[i + 1].ToArray().Length > 1 ? (T)Convert.ChangeType(arr[i + 1].ToArray()[1], typeof(T)) : default;
                response.Documents.Add(docId, primitive!);
            }

            return response;
        }
    }
}