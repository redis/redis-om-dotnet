using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly bool _saveState;
        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryProvider"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="stateManager">The state manager.</param>
        /// <param name="documentAttribute">the document attribute for the indexed type.</param>
        /// <param name="chunkSize">The size of chunks to use in pagination.</param>
        /// <param name="saveState">Whether or not to save state.</param>
        /// <param name="prefix">The prefix to use along with the data type.</param>
        internal RedisQueryProvider(IRedisConnection connection, RedisCollectionStateManager stateManager, DocumentAttribute documentAttribute, int chunkSize, bool saveState, string prefix)
        {
            Connection = connection;
            StateManager = stateManager;
            DocumentAttribute = documentAttribute;
            _chunkSize = chunkSize;
            _saveState = saveState;
            _prefix = prefix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryProvider"/> class.
        /// </summary>
        /// <param name="connection">the connection.</param>
        /// <param name="documentAttribute">The document attribute for the indexed type.</param>
        /// <param name="chunkSize">The size of chunks to use in pagination.</param>
        /// <param name="saveState">Whether or not to Save State.</param>
        /// <param name="prefix">The prefix to use along with the data type.</param>
        internal RedisQueryProvider(IRedisConnection connection, DocumentAttribute documentAttribute, int chunkSize, bool saveState, string prefix)
        {
            Connection = connection;
            DocumentAttribute = documentAttribute;
            StateManager = new RedisCollectionStateManager(DocumentAttribute);
            _chunkSize = chunkSize;
            _saveState = saveState;
            _prefix = prefix;
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

        /// <summary>
        /// Gets or sets the main boolean expression to be used for building the filter for this collection.
        /// </summary>
        internal Expression? BooleanExpression { get; set; }

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
            var booleanExpression = expression as Expression<Func<TElement, bool>>;
            return new RedisCollection<TElement>(this, expression, StateManager, booleanExpression, true, string.Empty);
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
        /// <param name="mainBooleanExpression">The main boolean expression to build the filter off of.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if indexed type not properly decorated.</exception>
        public SearchResponse<T> ExecuteQuery<T>(Expression expression, Expression? mainBooleanExpression)
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

            var query = ExpressionTranslator.BuildQueryFromExpression(expression, type, mainBooleanExpression, _prefix);
            var response = Connection.SearchRawResult(query);
            return new SearchResponse<T>(response);
        }

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="expression">The expression to be built and executed.</param>
        /// /// <param name="mainBooleanExpression">The main boolean expression to build the filter off of.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if indexed type not properly decorated.</exception>
        public async Task<SearchResponse<T>> ExecuteQueryAsync<T>(Expression expression, Expression? mainBooleanExpression)
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

            var query = ExpressionTranslator.BuildQueryFromExpression(expression, type, mainBooleanExpression, _prefix);
            var response = await Connection.SearchRawResultAsync(query);
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
            var reply = Connection.Execute("FT.AGGREGATE", aggregation.Serialize());
            var res = AggregationResult.FromRedisResult(reply);
            var reductionName = ((Reduction)aggregation.Predicates.Last()).ResultName;
            if (res.Any())
            {
                return res.First()[reductionName];
            }

            if (reductionName == "COUNT")
            {
                return reply.ToArray().First();
            }

            throw new Exception("Invalid value returned by server");
        }

        /// <summary>
        /// Executes an aggregation.
        /// </summary>
        /// <param name="expression">The expression to be built into a pipeline.</param>
        /// <param name="underpinningType">The indexed type underpinning the expression.</param>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <returns>The result of the aggregation.</returns>
        public async ValueTask<RedisReply> ExecuteReductiveAggregationAsync<T>(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var res = AggregationResult.FromRedisResult(await Connection.ExecuteAsync("FT.AGGREGATE", aggregation.Serialize()));
            var reductionName = ((Reduction)aggregation.Predicates.Last()).ResultName;

            return InvariantCultureResultParsing<T>(res.First()[reductionName]);
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

        private static RedisReply InvariantCultureResultParsing<T>(RedisReply value)
        {
            Type valueType = typeof(T);
            Type underlingValueType = Nullable.GetUnderlyingType(valueType);

            if (string.IsNullOrEmpty(value.ToString()) && underlingValueType != null)
            {
                return value;
            }

            /*
                When type of expected value is a double, float or decimal it must be parsed using InvariantCulture due some cultures use comma (",") or other than dot (".")
                because implicid casting of a TResult can produce an invalid value to cast.
                Value sometimes can be an int/long so value.ToString(..) is required before parsing as as double
            */

            if (valueType == typeof(double) || underlingValueType == typeof(double))
            {
                return double.Parse(value.ToString(CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(float) || underlingValueType == typeof(float))
            {
                return float.Parse(value.ToString(CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(decimal) || underlingValueType == typeof(decimal))
            {
                return float.Parse(value.ToString(CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(int) || underlingValueType == typeof(int))
            {
                return (int)value;
            }

            if (valueType == typeof(long) || underlingValueType == typeof(long))
            {
                return (int)value;
            }

            return value;
        }

        private TResult? First<TResult>(Expression expression)
            where TResult : notnull
        {
            var res = ExecuteQuery<TResult>(expression, BooleanExpression).Documents.First();
            SaveToStateManager(res.Key, res.Value);
            return res.Value;
        }

        private TResult? FirstOrDefault<TResult>(Expression expression)
            where TResult : notnull
        {
            var res = ExecuteQuery<TResult>(expression, BooleanExpression);
            if (res.Documents.Any())
            {
                var kvp = res.Documents.FirstOrDefault();
                SaveToStateManager(kvp.Key, kvp.Value);
                return kvp.Value;
            }

            return default;
        }

        private void SaveToStateManager(string key, object value)
        {
            if (_saveState)
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
