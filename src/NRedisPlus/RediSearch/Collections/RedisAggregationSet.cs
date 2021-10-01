using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public class RedisAggregationSet<T> : IQueryable<AggregationResult<T>>, IAsyncEnumerable<AggregationResult<T>>
    {
        public bool UseCursor { get; set; }
        private DocumentAttribute _rootAttribute;
        public RedisAggregationSet(IRedisConnection connection, bool useCursor = false)
        {
            var t = typeof(T);
            _rootAttribute = t.GetCustomAttribute<DocumentAttribute>();
            if (_rootAttribute == null)
                throw new ArgumentException("The root attribute of an AggregationSet must be decorated with a DocumentAttribute");
            Initalize(new RedisQueryProvider(connection, _rootAttribute), null, useCursor);
        }

        internal RedisAggregationSet(RedisQueryProvider provider, bool useCursor = false)
        {
            _rootAttribute = provider.DocumentAttribute;
            Initalize(provider, null, useCursor);
        }

        internal RedisAggregationSet(RedisQueryProvider provider, Expression expression, bool useCursor = false)
        {
            _rootAttribute = provider.DocumentAttribute;
            Initalize(provider, expression, useCursor);
        }

        internal RedisAggregationSet(RedisAggregationSet<T> source, Expression exp)
        {
            _rootAttribute = ((RedisQueryProvider)source.Provider).DocumentAttribute;
            Initalize((RedisQueryProvider)source.Provider, exp, source.UseCursor);
        }

        private void Initalize(RedisQueryProvider provider, Expression? expression, bool useCursor)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }            
            Provider = provider;
            Expression = expression ?? Expression.Constant(this);
            UseCursor = useCursor;
        }

        public Type ElementType { get => typeof(AggregationResult<T>); }

        public Expression Expression { get; private set; } = default!;

        public IQueryProvider Provider { get; private set; } = default!;

        public IEnumerator<AggregationResult<T>> GetEnumerator()
        {
            var provider = (RedisQueryProvider)Provider;
            return new AggregationEnumorator<T>(Expression, provider.Connection, useCursor: UseCursor);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var provider = (RedisQueryProvider)Provider;
            return new AggregationEnumorator<T>(Expression, provider.Connection, useCursor: UseCursor);
        }

        public IAsyncEnumerator<AggregationResult<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var provider = (RedisQueryProvider)Provider;
            return new AggregationEnumorator<T>(Expression, provider.Connection, useCursor: UseCursor);
        }

        public async ValueTask<List<AggregationResult<T>>> ToListAsync()
        {
            var enumorator = GetAsyncEnumerator();
            var retList = new List<AggregationResult<T>>();
            await foreach(var item in this)
            {
                retList.Add(item);
            }
            return retList;
        }

        public async ValueTask<AggregationResult<T>[]> ToArrayAsync() 
            => (await ToListAsync()).ToArray();


    }
}
