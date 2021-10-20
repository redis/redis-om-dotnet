using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Redis.OM.Contracts;
using Redis.OM.Model;
using Redis.OM.RediSearch.AggregationPredicates;
using Redis.OM.RediSearch.Collections;
using Redis.OM.RediSearch.Responses;
using Redis.OM.Schema;

namespace Redis.OM.RediSearch
{
    /// <summary>
    /// Query provider.
    /// </summary>
    internal class RedisQueryProvider : IQueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryProvider"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="stateManager">The state manager.</param>
        /// <param name="documentAttribute">the document attribute for the indexed type.</param>
        internal RedisQueryProvider(IRedisConnection connection, RedisCollectionStateManager stateManager, DocumentAttribute documentAttribute)
        {
            Connection = connection;
            StateManager = stateManager;
            DocumentAttribute = documentAttribute;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryProvider"/> class.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="documentAttribute">The document attribute for the indexed type.</param>
        internal RedisQueryProvider(IRedisConnection connection, DocumentAttribute documentAttribute)
        {
            Connection = connection;
            DocumentAttribute = documentAttribute;
            StateManager = new RedisCollectionStateManager(DocumentAttribute);
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

            if (attr == null || string.IsNullOrEmpty(attr.IndexName))
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
                    return ExecuteQuery<TResult>(expression).Documents.Values.FirstOrDefault() ?? default(TResult);
                case "First":
                    return ExecuteQuery<TResult>(expression).Documents.Values.First();
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
    }
}
