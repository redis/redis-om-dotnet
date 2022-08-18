using System.Globalization;

namespace Redis.OM
{
    /// <summary>
    /// A strong type class for mapping result from FT.INFO (see https://redis.io/commands/ft.info/ for detail).
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
                var value = responseArray[index].ToString(CultureInfo.InvariantCulture);

                switch (key)
                {
                    case "index_name": IndexName = value; break; // 1) and 2)
                    case "index_options": IndexOption = value; break; // 3) and 4)
                    case "fields": Fields = value; break; // 7) and 8)
                    case "num_docs": NumDocs = value; break; // 9) and 10)
                    case "max_doc_id": MaxDocId = value; break; // 11) and 12)
                    case "num_terms": NumTerms = value; break; // 13) and 14)
                    case "num_records": NumRecords = value; break; // 15) and 16)
                    case "inverted_sz_mb": InvertedSzMb = value; break; // 17) and 18)
                    case "vector_index_sz_mb": VectorIndexSzMb = value; break; // 19) and 20)
                    case "total_inverted_index_blocks": TotalInvertedIndexBlocks = value; break; // 21) and 22)
                    case "offset_vectors_sz_mb": OffsetVectorsSzMb = value; break; // 23) and 24)
                    case "doc_table_size_mb": DocTableSizeMb = value; break; // 25) and 26)
                    case "sortable_values_size_mb": SortableValuesSizeMb = value; break; // 27) and 28)
                    case "key_table_size_mb": KeyTableSizeMb = value; break; // 29) and 30)
                    case "records_per_doc_avg": RecordsPerDocAvg = value; break; // 31) and 32)
                    case "bytes_per_record_avg": BytesPerRecordAvg = value; break; // 33) and 34)
                    case "offsets_per_term_avg": OffsetsPerTermAvg = value; break; // 35) and 36)
                    case "offset_bits_per_record_avg": OffsetBitsPerRecordAvg = value; break; // 37) and 38)
                    case "hash_indexing_failures": HashIndexingFailures = value; break; // 39) and 40)
                    case "indexing": Indexing = value; break; // 41) and 42)
                    case "percent_indexed": PercentIndexed = value; break; // 43) and 44)
                    case "gc_stats": GcStats = value; break; // 45) and 46)
                    case "cursor_stats": CursorStats = value; break; // 47) and 48)
                    case "stopwords_list": StopwordsList = value; break; // 49) and 50)
                }
            }
        }

        /// <summary>
        /// Gets index_name.
        /// </summary>
        public string? IndexName { get; }

        /// <summary>
        /// Gets index_options.
        /// </summary>
        public string? IndexOption { get; }

        /// <summary>
        /// Gets fields.
        /// </summary>
        public string? Fields { get; }

        /// <summary>
        /// Gets num_docs.
        /// </summary>
        public string? NumDocs { get; }

        /// <summary>
        /// Gets max_doc_id.
        /// </summary>
        public string? MaxDocId { get; }

        /// <summary>
        /// Gets num_terms.
        /// </summary>
        public string? NumTerms { get; }

        /// <summary>
        /// Gets num_records.
        /// </summary>
        public string? NumRecords { get; }

        /// <summary>
        /// Gets inverted_sz_mb.
        /// </summary>
        public string? InvertedSzMb { get; }

        /// <summary>
        /// Gets vector_index_sz_mb.
        /// </summary>
        public string? VectorIndexSzMb { get; }

        /// <summary>
        /// Gets total_inverted_index_blocks.
        /// </summary>
        public string? TotalInvertedIndexBlocks { get; }

        /// <summary>
        /// Gets offset_vectors_sz_mb.
        /// </summary>
        public string? OffsetVectorsSzMb { get; }

        /// <summary>
        /// Gets doc_table_size_mb.
        /// </summary>
        public string? DocTableSizeMb { get; }

        /// <summary>
        /// Gets sortable_values_size_mb.
        /// </summary>
        public string? SortableValuesSizeMb { get; }

        /// <summary>
        /// Gets key_table_size_mb.
        /// </summary>
        public string? KeyTableSizeMb { get; }

        /// <summary>
        /// Gets records_per_doc_avg.
        /// </summary>
        public string? RecordsPerDocAvg { get; }

        /// <summary>
        /// Gets bytes_per_record_avg.
        /// </summary>
        public string? BytesPerRecordAvg { get; }

        /// <summary>
        /// Gets offsets_per_term_avg.
        /// </summary>
        public string? OffsetsPerTermAvg { get; }

        /// <summary>
        /// Gets offset_bits_per_record_avg.
        /// </summary>
        public string? OffsetBitsPerRecordAvg { get; }

        /// <summary>
        /// Gets hash_indexing_failures.
        /// </summary>
        public string? HashIndexingFailures { get; }

        /// <summary>
        /// Gets indexing.
        /// </summary>
        public string? Indexing { get; }

        /// <summary>
        /// Gets percent_indexed.
        /// </summary>
        public string? PercentIndexed { get; }

        /// <summary>
        /// Gets gc_stats.
        /// </summary>
        public string? GcStats { get; }

        /// <summary>
        /// Gets cursor_stats.
        /// </summary>
        public string? CursorStats { get; }

        /// <summary>
        /// Gets stopwords_list.
        /// </summary>
        public string? StopwordsList { get; }
    }
}
