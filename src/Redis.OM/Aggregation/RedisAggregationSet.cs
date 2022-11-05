using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;

namespace Redis.OM.Aggregation
{
    /// <summary>
    /// A collection that you can use to run aggregations in redis.
    /// </summary>
    /// <typeparam name="T">The type of the record shell in the aggregation result.</typeparam>
    public class RedisAggregationSet<T> : IQueryable<AggregationResult<T>>, IAsyncEnumerable<AggregationResult<T>>
    {
        private readonly int _chunkSize;
        private bool _useCursor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisAggregationSet{T}"/> class.
        /// </summary>
        /// <param name="connection">the connection to use.</param>
        /// <param name="useCursor">whether or not to use a cursor.</param>
        /// <param name="chunkSize">Size of the chunks to use during pagination, larger chunks return larger payloads but with fewer round trips.</param>
        public RedisAggregationSet(IRedisConnection connection, bool useCursor = false, int chunkSize = 1000)
        {
            var t = typeof(T);
            DocumentAttribute rootAttribute = t.GetCustomAttribute<DocumentAttribute>();
            if (rootAttribute == null)
            {
                throw new ArgumentException("The root attribute of an AggregationSet must be decorated with a DocumentAttribute");
            }

            _chunkSize = chunkSize;
            Initialize(new RedisQueryProvider(connection, rootAttribute, _chunkSize, true, string.Empty), null, useCursor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisAggregationSet{T}"/> class.
        /// </summary>
        /// <param name="provider">the query provider.</param>
        /// <param name="useCursor">whether or not to use the cursor.</param>
        /// <param name="chunkSize">Size of the chunks to use during pagination, larger chunks return larger payloads but with fewer round trips.</param>
        internal RedisAggregationSet(RedisQueryProvider provider, bool useCursor = false, int chunkSize = 1000)
        {
            _chunkSize = chunkSize;
            Initialize(provider, null, useCursor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisAggregationSet{T}"/> class.
        /// </summary>
        /// <param name="source">the old set.</param>
        /// <param name="exp">the new expression.</param>
        internal RedisAggregationSet(RedisAggregationSet<T> source, Expression exp)
        {
            _chunkSize = source._chunkSize;
            Initialize((RedisQueryProvider)source.Provider, exp, source._useCursor);
        }

        /// <inheritdoc/>
        public Type ElementType { get => typeof(AggregationResult<T>); }

        /// <inheritdoc/>
        public Expression Expression { get; private set; } = default!;

        /// <inheritdoc/>
        public IQueryProvider Provider { get; private set; } = default!;

        /// <inheritdoc/>
        public IEnumerator<AggregationResult<T>> GetEnumerator()
        {
            var provider = (RedisQueryProvider)Provider;
            return new AggregationEnumerator<T>(Expression, provider.Connection, useCursor: _useCursor, chunkSize: _chunkSize);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var provider = (RedisQueryProvider)Provider;
            return new AggregationEnumerator<T>(Expression, provider.Connection, useCursor: _useCursor, chunkSize: _chunkSize);
        }

        /// <inheritdoc/>
        public IAsyncEnumerator<AggregationResult<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var provider = (RedisQueryProvider)Provider;
            return new AggregationEnumerator<T>(Expression, provider.Connection, useCursor: _useCursor, chunkSize: _chunkSize);
        }

        /// <summary>
        /// Materializes the set as a list asynchronously.
        /// </summary>
        /// <returns>a task that will resolve when the list is enumerated.</returns>
        public async ValueTask<List<AggregationResult<T>>> ToListAsync()
        {
            var retList = new List<AggregationResult<T>>();
            await foreach (var item in this)
            {
                retList.Add(item);
            }

            return retList;
        }

        /// <summary>
        /// Materializes the set as an array asynchronously.
        /// </summary>
        /// <returns>a task that will resolve when the array is enumerated.</returns>
        public async ValueTask<AggregationResult<T>[]> ToArrayAsync()
            => (await ToListAsync()).ToArray();

        private void Initialize(RedisQueryProvider provider, Expression? expression, bool useCursor)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? Expression.Constant(this);
            _useCursor = useCursor;
        }
    }
}
