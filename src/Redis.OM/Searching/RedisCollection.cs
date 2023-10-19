using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Redis.OM.Common;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching.Query;
using StackExchange.Redis;

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

        private Expression<Func<T, bool>>? _booleanExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollection{T}"/> class.
        /// </summary>
        /// <param name="connection">Connection to Redis.</param>
        /// <param name="chunkSize">Size of chunks to pull back during pagination, defaults to 100.</param>
        public RedisCollection(IRedisConnection connection, int chunkSize = 100)
            : this(connection, true, chunkSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollection{T}"/> class.
        /// </summary>
        /// <param name="saveState">Determines whether or not the Redis Colleciton will maintain it's state internally.</param>
        /// <param name="connection">Connection to Redis.</param>
        /// <param name="chunkSize">Size of chunks to pull back during pagination, defaults to 100.</param>
        /// <exception cref="ArgumentException">Thrown if the root attribute of the Redis Colleciton is not decorated with a DocumentAttribute.</exception>
        public RedisCollection(IRedisConnection connection, bool saveState, int chunkSize)
        {
            var t = typeof(T);
            DocumentAttribute rootAttribute = t.GetCustomAttribute<DocumentAttribute>();
            if (rootAttribute == null)
            {
                throw new ArgumentException("The root attribute of a Redis Collection must be decorated with a DocumentAttribute");
            }

            ChunkSize = chunkSize;
            _connection = connection;
            SaveState = saveState;
            StateManager = new RedisCollectionStateManager(rootAttribute);
            Initialize(new RedisQueryProvider(connection, StateManager, rootAttribute, ChunkSize, SaveState), null, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCollection{T}"/> class.
        /// </summary>
        /// <param name="provider">Query Provider.</param>
        /// <param name="expression">Expression to be parsed for the query.</param>
        /// <param name="stateManager">Manager of the internal state of the collection.</param>
        /// <param name="saveState">Whether or not the StateManager will maintain the state.</param>
        /// <param name="chunkSize">Size of chunks to pull back during pagination, defaults to 100.</param>
        /// <param name="booleanExpression">The expression to build the filter from.</param>
        internal RedisCollection(RedisQueryProvider provider, Expression expression, RedisCollectionStateManager stateManager, Expression<Func<T, bool>>? booleanExpression, bool saveState, int chunkSize = 100)
        {
            StateManager = stateManager;
            _connection = provider.Connection;
            ChunkSize = chunkSize;
            SaveState = saveState;
            Initialize(provider, expression, booleanExpression);
        }

        /// <inheritdoc/>
        public Type ElementType => typeof(T);

        /// <inheritdoc/>
        public Expression Expression { get; private set; } = default!;

        /// <inheritdoc/>
        public IQueryProvider Provider { get; private set; } = default!;

        /// <inheritdoc />
        public bool SaveState { get; }

        /// <summary>
        /// Gets manages the state of the items queried from Redis.
        /// </summary>
        public RedisCollectionStateManager StateManager { get; }

        /// <inheritdoc />
        public int ChunkSize { get; }

        /// <summary>
        /// Gets or sets the root type for the collection.
        /// </summary>
        internal Type RootType { get; set; } = typeof(T);

        /// <summary>
        /// Gets or sets the main boolean expression to be used for building the filter for this collection.
        /// </summary>
        internal Expression<Func<T, bool>>? BooleanExpression
        {
            get
            {
                return _booleanExpression;
            }

            set
            {
                _booleanExpression = value;
                ((RedisQueryProvider)Provider).BooleanExpression = value;
            }
        }

        /// <inheritdoc />
        public bool Any()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)_connection.Search<T>(query).DocumentCount > 0;
        }

        /// <summary>
        /// Checks to see if anything matching the expression exists.
        /// </summary>
        /// <param name="expression">the expression to be matched.</param>
        /// <returns>Whether anything matching the expression was found.</returns>
        public bool Any(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)_connection.Search<T>(query).DocumentCount > 0;
        }

        /// <inheritdoc />
        public void Update(T item)
        {
            var key = item.GetKey();
            IList<IObjectDiff>? diff;
            var diffConstructed = StateManager.TryDetectDifferencesSingle(key, item, out diff);
            if (diffConstructed)
            {
                if (diff!.Any())
                {
                    var args = new List<string>();
                    var scriptName = diff!.First().Script;
                    foreach (var update in diff!)
                    {
                        args.AddRange(update.SerializeScriptArgs());
                    }

                    _connection.CreateAndEval(scriptName, new[] { key }, args.ToArray());
                }
            }
            else
            {
                _connection.UnlinkAndSet(key, item, StateManager.DocumentAttribute.StorageType);
            }

            SaveToStateManager(key, item);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(T item)
        {
            var key = item.GetKey();
            IList<IObjectDiff>? diff;
            var diffConstructed = StateManager.TryDetectDifferencesSingle(key, item, out diff);
            if (diffConstructed)
            {
                if (diff!.Any())
                {
                    var args = new List<string>();
                    var scriptName = diff!.First().Script;
                    foreach (var update in diff!)
                    {
                        args.AddRange(update.SerializeScriptArgs());
                    }

                    await _connection.CreateAndEvalAsync(scriptName, new[] { key }, args.ToArray());
                }
            }
            else
            {
                await _connection.UnlinkAndSetAsync(key, item, StateManager.DocumentAttribute.StorageType);
            }

            SaveToStateManager(key, item);
        }

        /// <inheritdoc />
        public async ValueTask UpdateAsync(IEnumerable<T> items)
        {
            var tasks = items.Select(UpdateAsyncNoSave);

            await Task.WhenAll(tasks);
            foreach (var kvp in tasks.Select(x => x.Result))
            {
                SaveToStateManager(kvp.Key, kvp.Value);
            }
        }

        /// <inheritdoc />
        public void Delete(T item)
        {
            var key = item.GetKey();
            _connection.Unlink(key);
            StateManager.Remove(key);
        }

        /// <inheritdoc />
        public void Delete(IEnumerable<T> items)
        {
            var keys = items.Select(x => x.GetKey()).ToArray();
            if (!keys.Any())
            {
                return;
            }

            foreach (var key in keys)
            {
                StateManager.Remove(key);
            }

            _connection.Unlink(keys);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(T item)
        {
            var key = item.GetKey();
            await _connection.UnlinkAsync(key);
            StateManager.Remove(key);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<T> items)
        {
            var keys = items.Select(x => x.GetKey()).ToArray();
            if (!keys.Any())
            {
                return;
            }

            foreach (var key in keys)
            {
                StateManager.Remove(key);
            }

            await _connection.UnlinkAsync(keys);
        }

        /// <inheritdoc />
        public async Task<IList<T>> ToListAsync()
        {
            var list = new List<T>();
            await foreach (var item in this)
            {
                list.Add(item);
            }

            return list;
        }

        /// <inheritdoc />
        public async Task<int> CountAsync()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)(await _connection.SearchAsync<T>(query)).DocumentCount;
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)(await _connection.SearchAsync<T>(query)).DocumentCount;
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)(await _connection.SearchAsync<T>(query)).DocumentCount > 0;
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)(await _connection.SearchAsync<T>(query)).DocumentCount > 0;
        }

        /// <inheritdoc />
        public async Task<T> FirstAsync()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            var result = res.Documents.First();
            SaveToStateManager(result.Key, result.Value);
            return result.Value;
        }

        /// <inheritdoc />
        public async Task<T> FirstAsync(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            var result = res.Documents.First();
            SaveToStateManager(result.Key, result.Value);
            return result.Value;
        }

        /// <inheritdoc />
        public async Task<T?> FirstOrDefaultAsync()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            var key = res.Documents.Keys.FirstOrDefault();
            if (key == default)
            {
                return default;
            }

            var result = res.Documents[key];
            SaveToStateManager(key, result);
            return result;
        }

        /// <inheritdoc />
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            var key = res.Documents.Keys.FirstOrDefault();
            if (key == default)
            {
                return default;
            }

            var result = res.Documents[key];
            SaveToStateManager(key, result);
            return result;
        }

        /// <inheritdoc />
        public async Task<T> SingleAsync()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            if (res.DocumentCount > 1)
            {
                throw new InvalidOperationException("Sequence contained more than one element.");
            }

            var key = res.Documents.Keys.Single();
            var result = res.Documents[key];
            SaveToStateManager(key, result);
            return result;
        }

        /// <inheritdoc />
        public async Task<T> SingleAsync(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            if (res.DocumentCount > 1)
            {
                throw new InvalidOperationException("Sequence contained more than one element.");
            }

            var key = res.Documents.Keys.Single();
            var result = res.Documents[key];
            SaveToStateManager(key, result);
            return result;
        }

        /// <inheritdoc />
        public async Task<T?> SingleOrDefaultAsync()
        {
            var query = ExpressionTranslator.BuildQueryFromExpression(Expression, typeof(T), BooleanExpression, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            if (res.DocumentCount != 1)
            {
                return default;
            }

            var key = res.Documents.Keys.SingleOrDefault();
            if (key != default)
            {
                var result = res.Documents[key];
                SaveToStateManager(key, result);
                return result;
            }

            return default;
        }

        /// <inheritdoc />
        public async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = await _connection.SearchAsync<T>(query);
            if (res.DocumentCount != 1)
            {
                return default;
            }

            var key = res.Documents.Keys.SingleOrDefault();
            if (key != null)
            {
                var result = res.Documents[key];
                SaveToStateManager(key, result);
                return result;
            }

            return default;
        }

        /// <inheritdoc />
        public int Count(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 0, Offset = 0 };
            return (int)_connection.Search<T>(query).DocumentCount;
        }

        /// <inheritdoc />
        public T First(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = _connection.Search<T>(query);
            var result = res.Documents.First();
            SaveToStateManager(result.Key, result.Value);
            return result.Value;
        }

        /// <inheritdoc />
        public T? FirstOrDefault(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = _connection.Search<T>(query);
            var result = res.Documents.FirstOrDefault();
            if (result.Key != null)
            {
                SaveToStateManager(result.Key, result.Value);
            }

            return result.Value;
        }

        /// <inheritdoc />
        public T Single(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = _connection.Search<T>(query);
            if (res.DocumentCount > 1)
            {
                throw new InvalidOperationException("Sequence contained more than one element.");
            }

            var result = res.Documents.Single();
            SaveToStateManager(result.Key, result.Value);
            return result.Value;
        }

        /// <inheritdoc />
        public T? SingleOrDefault(Expression<Func<T, bool>> expression)
        {
            var exp = Expression.Call(null, GetMethodInfo(this.Where, expression), new[] { Expression, Expression.Quote(expression) });
            var combined = BooleanExpression == null ? expression : BooleanExpression.And(expression);
            var query = ExpressionTranslator.BuildQueryFromExpression(exp, typeof(T), combined, RootType);
            query.Limit = new SearchLimit { Number = 1, Offset = 0 };
            var res = _connection.Search<T>(query);
            if (res.DocumentCount != 1)
            {
                return default;
            }

            var result = res.Documents.SingleOrDefault();
            SaveToStateManager(result.Key, result.Value);
            return result.Value;
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, T?>> FindByIdsAsync(IEnumerable<string> ids)
        {
            var tasks = new Dictionary<string, Task<T?>>();
            foreach (var id in ids.Distinct())
            {
                tasks.Add(id, FindByIdAsyncNoSave(id));
            }

            await Task.WhenAll(tasks.Values);
            var result = tasks.ToDictionary(x => x.Key, x => x.Value.Result);
            foreach (var res in result)
            {
                string? key;
                if (res.Value != null && res.Value.TryGetKey(out key) && key != null)
                {
                    SaveToStateManager((string)key, res.Value);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual IEnumerator<T> GetEnumerator()
        {
            StateManager.Clear();
            return new RedisCollectionEnumerator<T>(Expression, _connection, ChunkSize, StateManager, BooleanExpression, SaveState, RootType, typeof(T));
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

        /// <inheritdoc/>
        public void Save()
        {
            if (!SaveState)
            {
                throw new InvalidOperationException(
                    "The RedisCollection has been instructed to not maintain the state of records enumerated by " +
                    "Redis making the attempt to Save Invalid. Please initialize the RedisCollection with saveState " +
                    "set to true to Save documents in the RedisCollection");
            }

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
            if (!SaveState)
            {
                throw new InvalidOperationException(
                    "The RedisCollection has been instructed to not maintain the state of records enumerated by " +
                    "Redis making the attempt to Save Invalid. Please initialize the RedisCollection with saveState " +
                    "set to true to Save documents in the RedisCollection");
            }

            var diff = StateManager.DetectDifferences();
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
        public string Insert(T item, TimeSpan timeSpan)
        {
            return ((RedisQueryProvider)Provider).Connection.Set(item, timeSpan);
        }

        /// <inheritdoc/>
        public async Task<string> InsertAsync(T item)
        {
            return await ((RedisQueryProvider)Provider).Connection.SetAsync(item);
        }

        /// <inheritdoc/>
        public async Task<string> InsertAsync(T item, TimeSpan timeSpan)
        {
            return await ((RedisQueryProvider)Provider).Connection.SetAsync(item, timeSpan);
        }

        /// <inheritdoc/>
        public Task<string?> InsertAsync(T item, WhenKey when, TimeSpan? timeSpan = null)
        {
            return ((RedisQueryProvider)Provider).Connection.SetAsync(item, when, timeSpan);
        }

        /// <inheritdoc/>
        public string? Insert(T item, WhenKey when, TimeSpan? timeSpan = null)
        {
            return ((RedisQueryProvider)Provider).Connection.Set(item, when, timeSpan);
        }

        /// <inheritdoc/>
        public async Task<List<string>> InsertAsync(IEnumerable<T> items)
        {
            var distinct = items.Distinct().ToArray();
            if (!distinct.Any())
            {
                return new List<string>();
            }

            var tasks = new List<Task<string>>();
            foreach (var item in distinct)
            {
                tasks.Add(((RedisQueryProvider)Provider).Connection.SetAsync(item));
            }

            var result = await Task.WhenAll(tasks);
            return result.ToList();
        }

        /// <inheritdoc/>
        public async Task<List<string>> InsertAsync(IEnumerable<T> items, TimeSpan timeSpan)
        {
            var distinct = items.Distinct().ToArray();
            if (!distinct.Any())
            {
                return new List<string>();
            }

            var tasks = new List<Task<string>>();
            foreach (var item in distinct)
            {
                tasks.Add(((RedisQueryProvider)Provider).Connection.SetAsync(item, timeSpan));
            }

            var result = await Task.WhenAll(tasks);
            return result.ToList();
        }

        /// <inheritdoc/>
        public async Task<List<string?>> InsertAsync(IEnumerable<T> items, WhenKey when, TimeSpan? timeSpan = null)
        {
            var distinct = items.Distinct().ToArray();
            if (!distinct.Any())
            {
                return new List<string?>();
            }

            var tasks = new List<Task<string?>>();
            foreach (var item in distinct)
            {
                tasks.Add(((RedisQueryProvider)Provider).Connection.SetAsync(item, when, timeSpan));
            }

            var result = await Task.WhenAll(tasks);
            return result.ToList();
        }

        /// <inheritdoc/>
        public T? FindById(string id)
        {
            var prefix = typeof(T).GetKeyPrefix();
            string key = id.Contains(prefix) ? id : $"{prefix}:{id}";
            var result = _connection.Get<T>(key);
            if (result != null)
            {
                SaveToStateManager(key, result);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<T?> FindByIdAsync(string id)
        {
            var prefix = typeof(T).GetKeyPrefix();
            string key = id.Contains(prefix) ? id : $"{prefix}:{id}";
            var result = await _connection.GetAsync<T>(key);
            if (result != null)
            {
                SaveToStateManager(key, result);
            }

            return result;
        }

        /// <inheritdoc/>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var provider = (RedisQueryProvider)Provider;
            StateManager.Clear();
            return new RedisCollectionEnumerator<T>(Expression, provider.Connection, ChunkSize, StateManager, BooleanExpression, SaveState, RootType, typeof(T));
        }

        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused)
        {
            return f.Method;
        }

        private Task<T?> FindByIdAsyncNoSave(string id)
        {
            var prefix = typeof(T).GetKeyPrefix();
            string key = id.Contains(prefix) ? id : $"{prefix}:{id}";
            return _connection.GetAsync<T>(key).AsTask();
        }

        private async Task<KeyValuePair<string, T>> UpdateAsyncNoSave(T item)
        {
            var key = item.GetKey();
            IList<IObjectDiff>? diff;
            var diffConstructed = StateManager.TryDetectDifferencesSingle(key, item, out diff);
            if (diffConstructed)
            {
                if (diff!.Any())
                {
                    var args = new List<string>();
                    var scriptName = diff!.First().Script;
                    foreach (var update in diff!)
                    {
                        args.AddRange(update.SerializeScriptArgs());
                    }

                    await _connection.CreateAndEvalAsync(scriptName, new[] { key }, args.ToArray());
                }
            }
            else
            {
                await _connection.UnlinkAndSetAsync(key, item, StateManager.DocumentAttribute.StorageType);
            }

            return new KeyValuePair<string, T>(key, item);
        }

        private void Initialize(RedisQueryProvider provider, Expression? expression, Expression<Func<T, bool>>? booleanExpression)
        {
            if (expression != null && !typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException($"Not assignable from {expression.Type} expression");
            }

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? Expression.Constant(this);
            BooleanExpression = booleanExpression;
        }

        private void SaveToStateManager(string key, object value)
        {
            if (SaveState)
            {
                try
                {
                    StateManager.InsertIntoData(key, value);
                    StateManager.InsertIntoSnapshot(key, value);
                }
                catch (InvalidOperationException ex)
                {
                    throw new Exception(
                        "Exception encountered while trying to save State. This indicates a possible race condition. " +
                        "If you do not need to update, consider setting SaveState to false, otherwise, ensure collection is only enumerated on one thread at a time",
                        ex);
                }
            }
        }
    }
}