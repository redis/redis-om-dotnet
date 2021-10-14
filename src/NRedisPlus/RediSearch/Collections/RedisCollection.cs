using NRedisPlus.RediSearch;
using NRedisPlus.RediSearch.Enumorators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class RedisCollection<T> : IRedisCollection<T>, IAsyncEnumerable<T>
        where T : notnull
    {
        private IRedisConnection _connection;
        internal RedisCollectionStateManager StateManager { get; private set; }
        private DocumentAttribute _rootAttribute;
        public RedisCollection(IRedisConnection connection)
        {
            var t = typeof(T);
            _rootAttribute = t.GetCustomAttribute<DocumentAttribute>();
            if (_rootAttribute == null)
                throw new ArgumentException("The root attribute of a Redis Collection must be decorated with a DocumentAttribute");

            _connection = connection;
            StateManager = new RedisCollectionStateManager(_rootAttribute);
            Initalize(new RedisQueryProvider(connection, StateManager, _rootAttribute), null);
        }
        internal RedisCollection(RedisQueryProvider provider, Expression expression, RedisCollectionStateManager stateManager)
        {
            StateManager = stateManager;
            _connection = provider.Connection;
            _rootAttribute = provider.DocumentAttribute;
            Initalize(provider, expression);
        }

        private void Initalize(RedisQueryProvider provider, Expression? expression)
        {
            if (expression != null && !typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
                throw new ArgumentException($"Not assignable from {expression.Type} expression");
            Provider = provider ?? throw new ArgumentNullException("provider");
            Expression = expression ?? Expression.Constant(this);           
        }

        public bool Any(Expression<Func<T,bool>> expression)
        {
            var provider = ((RedisQueryProvider)Provider);
            var res = provider.ExecuteQuery<T>(expression);
            return res.Documents.Values.Any();
        }

        public Type ElementType => typeof(T);

        public Expression Expression { get; private set; } = default!;

        public IQueryProvider Provider { get; private set; } = default!;        

        public IEnumerator<T> GetEnumerator()
        {
            var provider = ((RedisQueryProvider)Provider);
            return new RedisCollectionEnumorator<T>(Expression, _connection, 100, StateManager);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

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
                    _connection.CreateAndEval(scriptName, new []{item.Key}, args.ToArray());
                }
            }
        }
        
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
                        continue;
                    
                    tasks.Add(_connection.CreateAndEvalAsync(scriptName, new []{item.Key}, args.ToArray()));
                }
            }
            await Task.WhenAll(tasks);
        }

        public string Insert(T item)
        {
            return ((RedisQueryProvider)Provider).Connection.Set(item);
        }
        
        public async Task<string> InsertAsync(T item)
        {
            return await ((RedisQueryProvider)Provider).Connection.SetAsync(item);
        }

        public T? FindById(string id) => _connection.Get<T>(id);
        public async Task<T?> FindByIdAsync(string id) => await _connection.GetAsync<T>(id);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var provider = ((RedisQueryProvider)Provider);
            return new RedisCollectionEnumorator<T>(Expression, provider.Connection, 100, StateManager);
        }
    }
}
