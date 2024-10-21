using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Redis.OM.Modeling;

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
            var infoIndex = 0;

            while (infoIndex < responseArray.Length - 1)
            {
                var key = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);
                infoIndex++;
                var value = responseArray[infoIndex];

                switch (key)
                {
                    case "index_name": IndexName = value.ToString(CultureInfo.InvariantCulture); break; // 1) and 2)
                    case "index_options": IndexOptions = new RedisIndexInfoIndexOptions(value); break; // 3) and 4)
                    case "index_definition": IndexDefinition = new RedisIndexInfoIndexDefinition(value); break; // 5) and 6)
                    case "fields": case "attributes": Attributes = value.ToArray().Select(x => new RedisIndexInfoAttribute(x)).ToArray(); break; // 7) and 8)
                    case "num_docs": NumDocs = value.ToString(CultureInfo.InvariantCulture); break; // 9) and 10)
                    case "max_doc_id": MaxDocId = value.ToString(CultureInfo.InvariantCulture); break; // 11) and 12)
                    case "num_terms": NumTerms = value.ToString(CultureInfo.InvariantCulture); break; // 13) and 14)
                    case "num_records": NumRecords = value.ToString(CultureInfo.InvariantCulture); break; // 15) and 16)
                    case "inverted_sz_mb": InvertedSzMb = value.ToString(CultureInfo.InvariantCulture); break; // 17) and 18)
                    case "vector_index_sz_mb": VectorIndexSzMb = value.ToString(CultureInfo.InvariantCulture); break; // 19) and 20)
                    case "total_inverted_index_blocks": TotalInvertedIndexBlocks = value.ToString(CultureInfo.InvariantCulture); break; // 21) and 22)
                    case "offset_vectors_sz_mb": OffsetVectorsSzMb = value.ToString(CultureInfo.InvariantCulture); break; // 23) and 24)
                    case "doc_table_size_mb": DocTableSizeMb = value.ToString(CultureInfo.InvariantCulture); break; // 25) and 26)
                    case "sortable_values_size_mb": SortableValuesSizeMb = value.ToString(CultureInfo.InvariantCulture); break; // 27) and 28)
                    case "key_table_size_mb": KeyTableSizeMb = value.ToString(CultureInfo.InvariantCulture); break; // 29) and 30)
                    case "records_per_doc_avg": RecordsPerDocAvg = value.ToString(CultureInfo.InvariantCulture); break; // 31) and 32)
                    case "bytes_per_record_avg": BytesPerRecordAvg = value.ToString(CultureInfo.InvariantCulture); break; // 33) and 34)
                    case "offsets_per_term_avg": OffsetsPerTermAvg = value.ToString(CultureInfo.InvariantCulture); break; // 35) and 36)
                    case "offset_bits_per_record_avg": OffsetBitsPerRecordAvg = value.ToString(CultureInfo.InvariantCulture); break; // 37) and 38)
                    case "hash_indexing_failures": HashIndexingFailures = value.ToString(CultureInfo.InvariantCulture); break; // 39) and 40)
                    case "indexing": Indexing = value.ToString(CultureInfo.InvariantCulture); break; // 41) and 42)
                    case "percent_indexed": PercentIndexed = value.ToString(CultureInfo.InvariantCulture); break; // 43) and 44)
                    case "gc_stats": GcStats = new RedisIndexInfoGcStats(value); break; // 45) and 46)
                    case "cursor_stats": CursorStats = new RedisIndexInfoCursorStats(value); break; // 47) and 48)
                    case "stopwords_list": StopwordsList = value.ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray(); break; // 49) and 50)
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
        public RedisIndexInfoIndexOptions? IndexOptions { get; }

        /// <summary>
        /// Gets index_definition.
        /// </summary>
        public RedisIndexInfoIndexDefinition? IndexDefinition { get; }

        /// <summary>
        /// Gets attributes. Note that it used to be called fields in the documentation.
        /// </summary>
        public RedisIndexInfoAttribute[]? Attributes { get; }

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
        public RedisIndexInfoGcStats? GcStats { get; }

        /// <summary>
        /// Gets cursor_stats.
        /// </summary>
        public RedisIndexInfoCursorStats? CursorStats { get; }

        /// <summary>
        /// Gets stopwords_list.
        /// </summary>
        public string[]? StopwordsList { get; }

        /// <summary>
        /// A strong type index_options, which is  4) on the list.
        /// </summary>
        public class RedisIndexInfoIndexOptions
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RedisIndexInfoIndexOptions"/> class.
            /// </summary>
            /// <param name="redisReply">result form FT.INFO idx line 4).</param>
            public RedisIndexInfoIndexOptions(RedisReply redisReply)
            {
            }
        }

        /// <summary>
        /// A strong type for an index_definition.
        /// </summary>
        public class RedisIndexInfoIndexDefinition
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RedisIndexInfoIndexDefinition"/> class.
            /// </summary>
            /// <param name="redisReply">result form FT.INFO idx line 6).</param>
            public RedisIndexInfoIndexDefinition(RedisReply redisReply)
            {
                var responseArray = redisReply.ToArray();
                var infoIndex = 0;

                while (infoIndex < responseArray.Length - 1)
                {
                    var key = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);
                    infoIndex++;
                    var value = responseArray[infoIndex];

                    switch (key)
                    {
                        case "key_type": Identifier = value.ToString(CultureInfo.InvariantCulture); break;
                        case "prefixes": Prefixes = value.ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray(); break;
                        case "default_score": DefaultScore = value.ToString(CultureInfo.InvariantCulture); break;
                        case "default_language": DefaultLanguage = value.ToString(CultureInfo.InvariantCulture); break;
                        case "filter": Filter = value.ToString(CultureInfo.InvariantCulture); break;
                        case "language_field": LanguageField = value.ToString(CultureInfo.InvariantCulture); break;
                    }
                }
            }

            /// <summary>
            /// Gets key_type.
            /// </summary>
            public string? Identifier { get; }

            /// <summary>
            /// Gets prefixes.
            /// </summary>
            public string[]? Prefixes { get; }

            /// <summary>
            /// Gets default_score.
            /// </summary>
            public string? DefaultScore { get; }

            /// <summary>
            /// Gets Filter.
            /// </summary>
            public string? Filter { get; }

            /// <summary>
            /// Gets language.
            /// </summary>
            public string? DefaultLanguage { get; }

            /// <summary>
            /// Gets LanguageField.
            /// </summary>
            public string? LanguageField { get; }
        }

        /// <summary>
        /// A strong type for an attribute.
        /// </summary>
        public class RedisIndexInfoAttribute
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RedisIndexInfoAttribute"/> class.
            /// </summary>
            /// <param name="redisReply">result form FT.INFO idx line 8).</param>
            public RedisIndexInfoAttribute(RedisReply redisReply)
            {
                var responseArray = redisReply.ToArray();
                var infoIndex = 0;

                while (infoIndex < responseArray.Length - 1)
                {
                    var key = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);
                    infoIndex++;
                    var value = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);

                    switch (key)
                    {
                        case "identifier": Identifier = value; break;
                        case "attribute": Attribute = value; break;
                        case "type": Type = value; break;
                        case "SEPARATOR": Separator = value; break;
                        case "algorithm": Algorithm = value; break;
                        case "data_type": VectorType = value; break;
                        case "dim": Dimension = value; break;
                        case "distance_metric": DistanceMetric = value; break;
                        case "M": M = value; break;
                        case "ef_construction": EfConstruction = value; break;
                        case "WEIGHT": Weight = value; break;
                    }
                }

                if (responseArray.Any(x => ((string)x).Equals("NOSTEM", StringComparison.InvariantCultureIgnoreCase)))
                {
                    NoStem = true;
                }

                if (responseArray.Select(x => x.ToString())
                    .Any(x => x.Equals("SORTABLE", StringComparison.InvariantCultureIgnoreCase)))
                {
                    Sortable = true;
                }
            }

            /// <summary>
            /// Gets identifier.
            /// </summary>
            public string? Identifier { get; }

            /// <summary>
            /// Gets attribute.
            /// </summary>
            public string? Attribute { get; }

            /// <summary>
            /// Gets type.
            /// </summary>
            public string? Type { get; }

            /// <summary>
            /// Gets SEPARATOR.
            /// </summary>
            public string? Separator { get; }

            /// <summary>
            /// Gets SORTABLE.
            /// </summary>
            public bool? Sortable { get; }

            /// <summary>
            /// Gets NOSTEM.
            /// </summary>
            public bool? NoStem { get; }

            /// <summary>
            /// Gets weight.
            /// </summary>
            public string? Weight { get; }

            /// <summary>
            /// Gets Algorithm.
            /// </summary>
            public string? Algorithm { get; }

            /// <summary>
            /// Gets the VectorType.
            /// </summary>
            public string? VectorType { get; }

            /// <summary>
            /// Gets Dimension.
            /// </summary>
            public string? Dimension { get; }

            /// <summary>
            /// Gets DistanceMetric.
            /// </summary>
            public string? DistanceMetric { get; }

            /// <summary>
            /// Gets M.
            /// </summary>
            public string? M { get; }

            /// <summary>
            /// Gets EF constructor.
            /// </summary>
            public string? EfConstruction { get; }
        }

        /// <summary>
        /// A strong type for gc_stats, which is  46) on the list.
        /// </summary>
        public class RedisIndexInfoGcStats
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RedisIndexInfoGcStats"/> class.
            /// </summary>
            /// <param name="redisReply">result form FT.INFO idx line 46).</param>
            public RedisIndexInfoGcStats(RedisReply redisReply)
            {
                var responseArray = redisReply.ToArray();
                var infoIndex = 0;

                while (infoIndex < responseArray.Length - 1)
                {
                    var key = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);
                    infoIndex++;
                    var value = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);

                    switch (key)
                    {
                        case "bytes_collected": // 46) 1) and 48) 2)
                            BytesCollected = value;
                            break;
                        case "total_ms_run": // 46) 3) and 48) 4)
                            TotalMsRun = value;
                            break;
                        case "total_cycles": // 46) 5) and 48) 6)
                            TotalCycles = value;
                            break;
                        case "average_cycle_time_ms": // 46) 7) and 48) 8)
                            AverageCycleTimeMs = value;
                            break;
                        case "last_run_time_ms": // 46) 9) and 48) 10)
                            LastRunTimeMs = value;
                            break;
                        case "gc_numeric_trees_missed": // 46) 11) and 48) 12)
                            GcNumericTreesMissed = value;
                            break;
                        case "gc_blocks_denied": // 46) 13) and 48) 14)
                            GcBlocksDenied = value;
                            break;
                    }
                }
            }

            /// <summary>
            /// Gets bytes_collected.
            /// </summary>
            public string? BytesCollected { get; }

            /// <summary>
            /// Gets total_ms_run.
            /// </summary>
            public string? TotalMsRun { get; }

            /// <summary>
            /// Gets total_cycles.
            /// </summary>
            public string? TotalCycles { get; }

            /// <summary>
            /// Gets average_cycle_time_ms.
            /// </summary>
            public string? AverageCycleTimeMs { get; }

            /// <summary>
            /// Gets last_run_time_ms.
            /// </summary>
            public string? LastRunTimeMs { get; }

            /// <summary>
            /// Gets gc_numeric_trees_missed.
            /// </summary>
            public string? GcNumericTreesMissed { get; }

            /// <summary>
            /// Gets gc_blocks_denied.
            /// </summary>
            public string? GcBlocksDenied { get; }
        }

        /// <summary>
        /// A strong type for cursor_stats, which is  46) on the list.
        /// </summary>
        public class RedisIndexInfoCursorStats
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RedisIndexInfoCursorStats"/> class.
            /// </summary>
            /// <param name="redisReply">result form FT.INFO idx line 48).</param>
            public RedisIndexInfoCursorStats(RedisReply redisReply)
            {
                var responseArray = redisReply.ToArray();
                var infoIndex = 0;

                while (infoIndex < responseArray.Length - 1)
                {
                    var key = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);
                    infoIndex++;
                    var value = responseArray[infoIndex].ToString(CultureInfo.InvariantCulture);

                    switch (key)
                    {
                        case "global_idle": // 48) 1) and 48) 2)
                            GlobalIdle = long.TryParse(value, out var globalIdle) ? globalIdle : 0;
                            break;
                        case "global_total": // 48) 3) and 48) 4)
                            GlobalTotal = long.TryParse(value, out var globalTotal) ? globalTotal : 0;
                            break;
                        case "index_capacity": // 48) 5) and 48) 6)
                            IndexCapacity = long.TryParse(value, out var indexCapacity) ? indexCapacity : 0;
                            break;
                        case "index_total": // 48) 7) and 48) 8)
                            IndexTotal = long.TryParse(value, out var indexTotal) ? indexTotal : 0;
                            break;
                    }
                }
            }

            /// <summary>
            /// Gets global_idle.
            /// </summary>
            public long GlobalIdle { get; }

            /// <summary>
            /// Gets global_total.
            /// </summary>
            public long GlobalTotal { get; }

            /// <summary>
            /// Gets index_capacity.
            /// </summary>
            public long IndexCapacity { get; }

            /// <summary>
            /// Gets index_total.
            /// </summary>
            public long IndexTotal { get; }
        }
    }
}
