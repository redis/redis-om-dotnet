using System.Globalization;

namespace Redis.OM
{
    /// <summary>
    /// A strong type class for mapping result from FT.INFO.
    /// </summary>
    public class RedisIndexInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisIndexInfo"/> class.
        /// </summary>
        /// <param name="redisReply">result form FT.INFO idx.</param>
        public RedisIndexInfo(RedisReply redisReply)
        {
            var responseArray = redisReply.ToArray();
            var index = 0;

            while (index < responseArray.Length - 1)
            {
                var key = responseArray[index].ToString(CultureInfo.InvariantCulture);
                index++;
                var value = responseArray[index].ToString();

                switch (key)
                {
                    case "index_name":
                        IndexName = value.ToString(CultureInfo.InvariantCulture);
                        break;
                    case "num_docs":
                        NumDocs = int.TryParse(value, out var numDocs) ? numDocs : null;
                        break;
                    case "num_terms":
                        NumTerms = int.TryParse(value, out var numTerms) ? numTerms : null;
                        break;
                    case "num_records":
                        NumRecords = int.TryParse(value, out var numRecords) ? numRecords : null;
                        break;
                    case "indexing":
                        Indexing = int.TryParse(value, out var indexing) ? indexing : null;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets index_name.
        /// </summary>
        public string? IndexName { get;  }

        /// <summary>
        /// Gets num_docs.
        /// </summary>
        public int? NumDocs { get;  }

        /// <summary>
        /// Gets num_terms.
        /// </summary>
        public int? NumTerms { get;  }

        /// <summary>
        /// Gets num_records.
        /// </summary>
        public int? NumRecords { get;  }

        /// <summary>
        /// Gets indexing.
        /// </summary>
        public int? Indexing { get;  }
    }
}
