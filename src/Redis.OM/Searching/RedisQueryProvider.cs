using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM.Common;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Searching
{
    /// <summary>
    /// Query provider.
    /// </summary>
    internal class RedisQueryProvider : IQueryProvider
    {
        private readonly int _chunkSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryProvider"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="stateManager">The state manager.</param>
        /// <param name="documentAttribute">the document attribute for the indexed type.</param>
        /// <param name="chunkSize">The size of chunks to use in pagination.</param>
        internal RedisQueryProvider(IRedisConnection connection, RedisCollectionStateManager stateManager, DocumentAttribute documentAttribute, int chunkSize)
        {
            Connection = connection;
            StateManager = stateManager;
            DocumentAttribute = documentAttribute;
            _chunkSize = chunkSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryProvider"/> class.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="documentAttribute">The document attribute for the indexed type.</param>
        /// <param name="chunkSize">The size of chunks to use in pagination.</param>
        internal RedisQueryProvider(IRedisConnection connection, DocumentAttribute documentAttribute, int chunkSize)
        {
            Connection = connection;
            DocumentAttribute = documentAttribute;
            StateManager = new RedisCollectionStateManager(DocumentAttribute);
            _chunkSize = chunkSize;
        }

        /// <summary>
        /// Gets the connection to redis.
        /// </summary>
        internal IRedisConnection Connection { get; }

        /// <summary>
        /// Gets or sets the state manager.
        /// </summary>
        internal RedisCollectionStateManager StateManager { get; set; }

        /// <summary>
        /// Gets the document attribute.
        /// </summary>
        internal DocumentAttribute DocumentAttribute { get; }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type;
            try
            {
                return
                   (IQueryable)Activator.CreateInstance(
                       typeof(RedisCollection<>).MakeGenericType(elementType),
                       this,
                       expression);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException == null)
                {
                    throw;
                }

                throw e.InnerException;
            }
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            where TElement : notnull
        {
            return new RedisCollection<TElement>(this, expression, StateManager);
        }

        /// <inheritdoc/>
        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="expression">The expression to be built and executed.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if indexed type not properly decorated.</exception>
        public SearchResponse<T> ExecuteQuery<T>(Expression expression)
            where T : notnull
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<DocumentAttribute>();
            if (attr == null)
            {
                type = GetRootType((MethodCallExpression)expression);
                attr = type.GetCustomAttribute<DocumentAttribute>();
            }

            if (attr == null)
            {
                throw new InvalidOperationException("Searches can only be performed on objects decorated with a RedisObjectDefinitionAttribute that specifies a particular index");
            }

            var query = ExpressionTranslator.BuildQueryFromExpression(expression, type);
            var response = Connection.SearchRawResult(query);
            return new SearchResponse<T>(response);
        }

        /// <summary>
        /// Executes an aggregation.
        /// </summary>
        /// <param name="expression">The expression to be built into a pipeline.</param>
        /// <param name="underpinningType">The indexed type underpinning the expression.</param>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <returns>The result of the aggregation.</returns>
        public IEnumerable<AggregationResult<T>> ExecuteAggregation<T>(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var res = Connection.Execute("FT.AGGREGATE", aggregation.Serialize());
            return AggregationResult<T>.FromRedisResult(res);
        }

        /// <summary>
        /// Executes an aggregation.
        /// </summary>
        /// <param name="expression">The expression to be built into a pipeline.</param>
        /// <param name="underpinningType">The indexed type underpinning the expression.</param>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <returns>The result of the aggregation.</returns>
        public async Task<IEnumerable<AggregationResult<T>>> ExecuteAggregationAsync<T>(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var res = await Connection.ExecuteAsync("FT.AGGREGATE", aggregation.Serialize());
            return AggregationResult<T>.FromRedisResult(res);
        }

        /// <summary>
        /// Executes an aggregation.
        /// </summary>
        /// <param name="expression">The expression to be built into a pipeline.</param>
        /// <param name="underpinningType">The indexed type underpinning the expression.</param>
        /// <returns>The result of the aggregation.</returns>
        public RedisReply ExecuteReductiveAggregation(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var res = AggregationResult.FromRedisResult(Connection.Execute("FT.AGGREGATE", aggregation.Serialize()));
            var reductionName = ((Reduction)aggregation.Predicates.Last()).ResultName;
            return res.First()[reductionName];
        }

        /// <summary>
        /// Executes an aggregation.
        /// </summary>
        /// <param name="expression">The expression to be built into a pipeline.</param>
        /// <param name="underpinningType">The indexed type underpinning the expression.</param>
        /// <returns>The result of the aggregation.</returns>
        public async ValueTask<RedisReply> ExecuteReductiveAggregationAsync(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var res = AggregationResult.FromRedisResult(await Connection.ExecuteAsync("FT.AGGREGATE", aggregation.Serialize()));
            var reductionName = ((Reduction)aggregation.Predicates.Last()).ResultName;
            return res.First()[reductionName];
        }

        /// <summary>
        /// Gets the root type for the expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The root type.</returns>
        public Type GetRootType(MethodCallExpression expression)
        {
            while (expression.Arguments[0] is MethodCallExpression innerExpression)
            {
                expression = innerExpression;
            }

            return expression.Arguments[0].Type.GenericTypeArguments[0];
        }

        /// <inheritdoc/>
        public TResult? Execute<TResult>(Expression expression)
        where TResult : notnull
        {
            if (expression is not MethodCallExpression methodCall)
            {
                throw new NotImplementedException();
            }

            switch (methodCall.Method.Name)
            {
                case "FirstOrDefault":
                    return FirstOrDefault<TResult>(expression);
                case "First":
                    return First<TResult>(expression);
                case "Sum":
                case "Min":
                case "Max":
                case "Average":
                case "Count":
                case "LongCount":
                    var elementType = GetRootType(methodCall);
                    var res = ExecuteReductiveAggregation(methodCall, elementType);
                    return (TResult)Convert.ChangeType(res, typeof(TResult));
            }

            throw new NotImplementedException();
        }

        private TResult? First<TResult>(Expression expression)
            where TResult : notnull
        {
            var res = ExecuteQuery<TResult>(expression).Documents.First();
            StateManager.InsertIntoData(res.Key, res.Value);
            StateManager.InsertIntoSnapshot(res.Key, res.Value);
            return res.Value;
        }

        private TResult? FirstOrDefault<TResult>(Expression expression)
            where TResult : notnull
        {
            var res = ExecuteQuery<TResult>(expression);
            if (res.Documents.Any())
            {
                var kvp = res.Documents.FirstOrDefault();
                StateManager.InsertIntoSnapshot(kvp.Key, kvp.Value);
                StateManager.InsertIntoData(kvp.Key, kvp.Value);
                return kvp.Value;
            }

            return default;
        }
    }
}
