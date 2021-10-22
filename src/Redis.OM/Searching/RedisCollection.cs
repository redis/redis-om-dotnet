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

namespace Redis.OM.Searching
{
    /// <summary>
    /// Collection of items in Redis, can be queried using it's fluent interface.
    /// </summary>
    /// <typeparam name="T">The type being stored in Redis.</typeparam>
    public class RedisCollection<T> : IRedisCollection<T>
        where T : notnull
    {
        private readonly IRedisConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollection{T}"/> class.
        /// </summary>
        /// <param name="connection">Connection to Redis.</param>
        public RedisCollection(IRedisConnection connection)
        {
            var t = typeof(T);
            DocumentAttribute rootAttribute = t.GetCustomAttribute<DocumentAttribute>();
            if (rootAttribute == null)
            {
                throw new ArgumentException("The root attribute of a Redis Collection must be decorated with a DocumentAttribute");
            }

            _connection = connection;
            StateManager = new RedisCollectionStateManager(rootAttribute);
            Initialize(new RedisQueryProvider(connection, StateManager, rootAttribute), null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollection{T}"/> class.
        /// </summary>
        /// <param name="provider">Query Provider.</param>
        /// <param name="expression">Expression to be parsed for the query.</param>
        /// <param name="stateManager">Manager of the internal state of the collection.</param>
        internal RedisCollection(RedisQueryProvider provider, Expression expression, RedisCollectionStateManager stateManager)
        {
            StateManager = stateManager;
            _connection = provider.Connection;
            Initialize(provider, expression);
        }

        /// <inheritdoc/>
        public Type ElementType => typeof(T);

        /// <inheritdoc/>
        public Expression Expression { get; private set; } = default!;

        /// <inheritdoc/>
        public IQueryProvider Provider { get; private set; } = default!;

        /// <summary>
        /// Gets manages the state of the items queried from Redis.
        /// </summary>
        public RedisCollectionStateManager StateManager { get; }

        /// <summary>
        /// Checks to see if anything matching the expression exists.
        /// </summary>
        /// <param name="expression">the expression to be matched.</param>
        /// <returns>Whether anything matching the expression was found.</returns>
        public bool Any(Expression<Func<T, bool>> expression)
        {
            var provider = (RedisQueryProvider)Provider;
            var res = provider.ExecuteQuery<T>(expression);
            return res.Documents.Values.Any();
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return new RedisCollectionEnumerator<T>(Expression, _connection, 100, StateManager);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

        /// <inheritdoc/>
        public void Save()
        {
            var diff = StateManager.DetectDifferences();
            foreach (var item in diff)
            {
                if (item.Value.Any())
                {
                    var args = new List<string>();
                    var scriptName = item.Value.First().Script;
                    foreach (var update in item.Value)
                    {
                        args.AddRange(update.SerializeScriptArgs());
                    }

                    _connection.CreateAndEval(scriptName, new[] { item.Key }, args.ToArray());
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask SaveAsync()
        {
            var diff = StateManager.DetectDifferences();
            Console.WriteLine(diff);
            var tasks = new List<Task<int?>>();
            foreach (var item in diff)
            {
                if (item.Value.Any())
                {
                    var args = new List<string>();
                    var scriptName = item.Value.First().Script;
                    foreach (var update in item.Value)
                    {
                        args.AddRange(update.SerializeScriptArgs());
                    }

                    if (item.Value.First() is HashDiff && args.Count <= 2)
                    {
                        continue;
                    }

                    tasks.Add(_connection.CreateAndEvalAsync(scriptName, new[] { item.Key }, args.ToArray()));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public string Insert(T item)
        {
            return ((RedisQueryProvider)Provider).Connection.Set(item);
        }

        /// <inheritdoc/>
        public async Task<string> InsertAsync(T item)
        {
            return await ((RedisQueryProvider)Provider).Connection.SetAsync(item);
        }

        /// <inheritdoc/>
        public T? FindById(string id) => _connection.Get<T>(id);

        /// <inheritdoc/>
        public async Task<T?> FindByIdAsync(string id) => await _connection.GetAsync<T>(id);

        /// <inheritdoc/>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var provider = (RedisQueryProvider)Provider;
            return new RedisCollectionEnumerator<T>(Expression, provider.Connection, 100, StateManager);
        }

        private void Initialize(RedisQueryProvider provider, Expression? expression)
        {
            if (expression != null && !typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException($"Not assignable from {expression.Type} expression");
            }

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? Expression.Constant(this);
        }
    }
}
