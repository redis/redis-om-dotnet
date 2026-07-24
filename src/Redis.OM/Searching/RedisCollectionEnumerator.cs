using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Redis.OM;
using Redis.OM.Common;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching.Query;

namespace Redis.OM.Searching
{
    /// <summary>
    /// Enumerator for collection.
    /// </summary>
    /// <typeparam name="T">the indexed type.</typeparam>
    internal class RedisCollectionEnumerator<T> : IEnumerator<T>, IAsyncEnumerator<T>
        where T : notnull
    {
        private readonly RedisQuery _query;
        private readonly Type? _primitiveType;
        private readonly bool _limited;
        private readonly bool _saveState;
        private readonly IRedisConnection _connection;
        private readonly RedisCollectionStateManager _stateManager;
        private SearchResponse<T> _records = new (new RedisReply(new RedisReply[] { 0 }));
        private bool _started;
        private int _index = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollectionEnumerator{T}"/> class.
        /// </summary>
        /// <param name="exp">expression to materialize.</param>
        /// <param name="connection">the connection to redis.</param>
        /// <param name="chunkSize">the size of a chunk to pull back.</param>
        /// <param name="stateManager">the state manager.</param>
        /// <param name="booleanExpression">The main boolean expression to use to build the filter.</param>
        /// <param name="saveState">Determins whether the records from the RedisCollection are stored in the StateManager.</param>
        /// <param name="rootType">The root type for the enumerator.</param>
        /// <param name="type">The type the enumerator is responsible for enumerating.</param>
        internal RedisCollectionEnumerator(Expression exp, IRedisConnection connection, int chunkSize, RedisCollectionStateManager stateManager, Expression<Func<T, bool>>? booleanExpression, bool saveState, Type rootType, Type type)
        {
            if (!RedisSchemaField.IsComplexType(type))
            {
                _primitiveType = type;
            }

            _query = ExpressionTranslator.BuildQueryFromExpression(exp, rootType, booleanExpression, rootType);
            if (_query.Limit != null)
            {
                _limited = true;
            }
            else
            {
                _query.Limit = new SearchLimit { Offset = 0, Number = chunkSize };
            }

            _connection = connection;
            _stateManager = stateManager;
            _saveState = saveState;
        }

        /// <summary>
        /// Gets current record.
        /// </summary>
        public T Current => _records[_index];

        /// <inheritdoc/>
        object IEnumerator.Current => _records[_index];

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
#pragma warning disable 1998
        public async ValueTask DisposeAsync()
#pragma warning restore 1998
        {
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index + 1 < _records.Documents.Count)
            {
                _index += 1;
                return true;
            }

            if (ShouldStopPaging())
            {
                return false;
            }

            return GetNextChunk();
        }

        /// <inheritdoc/>
        public async ValueTask<bool> MoveNextAsync()
        {
            if (_index + 1 < _records.Documents.Count)
            {
                _index += 1;
                return true;
            }

            if (ShouldStopPaging())
            {
                return false;
            }

            return await GetNextChunkAsync();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _started = false;
            _index = -1;
            _records = new SearchResponse<T>(new RedisReply(new RedisReply[] { 0 }));
            if (_query.Limit != null)
            {
                _query.Limit.Offset = 0;
            }
        }

        private static Type GetRootType(MethodCallExpression expression)
        {
            while (expression.Arguments[0] is MethodCallExpression innerExpression)
            {
                expression = innerExpression;
            }

            return expression.Arguments[0].Type.GenericTypeArguments[0];
        }

        /// <summary>
        /// Determines whether pagination should stop rather than fetch another chunk from the server.
        /// </summary>
        /// <remarks>
        /// Alongside the normal "the server returned a short page" termination, paging also stops once a
        /// single page has handed back as many documents as the search advertised in total
        /// (<see cref="SearchResponse{T}.DocumentCount"/>) — at that point the whole result set is in
        /// hand. On a well-behaved server this only ever coincides with a result set that fits in one
        /// page (a page can never exceed the requested <c>Number</c>), so it does not change multi-page
        /// behaviour. It does, however, terminate a server that ignores the <c>LIMIT</c> offset and
        /// returns an ever-growing <c>[0, offset+Number)</c> window (the RESP3 clustered
        /// <c>FT.SEARCH</c> coordinator bug fixed in RediSearch#10394), which would otherwise page until
        /// the server rejects the offset ("LIMIT exceeds maximum of 1000000").
        /// </remarks>
        /// <returns>Whether to stop paging.</returns>
        private bool ShouldStopPaging()
        {
            if (!_started)
            {
                return false;
            }

            if (_limited)
            {
                return true;
            }

            // Documents skipped in this chunk (e.g. keys that expired mid-search) mean the real
            // result set is still being drained, so keep paging.
            if (_records.DocumentsSkippedCount != 0)
            {
                return false;
            }

            // The server returned a short page, so there is nothing left to fetch.
            if (_records.Documents.Count < _query!.Limit!.Number)
            {
                return true;
            }

            // A single page returned the whole advertised result set — we already have everything.
            return _records.Documents.Count >= _records.DocumentCount;
        }

        /// <summary>
        /// Determines the index within a freshly fetched chunk at which to begin surfacing documents.
        /// </summary>
        /// <remarks>
        /// A well-behaved server applies the <c>LIMIT</c> offset and returns the window
        /// <c>[offset, offset+Number)</c>, so iteration starts at the front of the chunk (0). A server
        /// that ignores the offset returns the prefix <c>[0, offset+Number)</c> instead — detectable
        /// because it hands back more rows than were requested — so the rows for this page begin at the
        /// requested offset within the returned set. Starting there skips the prefix already surfaced by
        /// earlier pages, so each match is surfaced exactly once.
        /// </remarks>
        /// <returns>The starting index for the current chunk.</returns>
        private int StartIndexForChunk()
        {
            return _records.Documents.Count > _query!.Limit!.Number ? _query!.Limit!.Offset : 0;
        }

        private bool GetNextChunk()
        {
            if (_started)
            {
                _query!.Limit!.Offset = _query.Limit.Offset + _query.Limit.Number;
            }

            var res = _connection.SearchRawResult(_query);
            _records = new SearchResponse<T>(res);
            _index = StartIndexForChunk();
            _started = true;
            ConcatenateRecords();
            return _index < _records.Documents.Count;
        }

        private async ValueTask<bool> GetNextChunkAsync()
        {
            if (_started)
            {
                _query!.Limit!.Offset = _query.Limit.Offset + _query.Limit.Number;
            }

            _records = await _connection.SearchAsync<T>(_query).ConfigureAwait(false);
            _index = StartIndexForChunk();
            _started = true;
            ConcatenateRecords();
            return _index < _records.Documents.Count;
        }

        private void ConcatenateRecords()
        {
            if (!_saveState)
            {
                return;
            }

            foreach (var record in _records.Documents)
            {
                if (_stateManager.Data.ContainsKey(record.Key) || _primitiveType != null)
                {
                    continue;
                }

                _stateManager.InsertIntoData(record.Key, record.Value);
                _stateManager.InsertIntoSnapshot(record.Key, record.Value);
            }
        }
    }
}
