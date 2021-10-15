using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NRedisPlus.Contracts;
using NRedisPlus.RediSearch;

namespace NRedisPlus.RediSearch
{
    internal class RedisQueryProvider : IQueryProvider
    {
        internal IRedisConnection Connection { get; private set; }
        internal RedisCollectionStateManager StateManager { get; set; }
        internal DocumentAttribute DocumentAttribute { get; private set; }
        internal RedisQueryProvider(IRedisConnection connection, RedisCollectionStateManager stateManager, DocumentAttribute documentAttribute)
        {
            Connection = connection;
            StateManager = stateManager;
            DocumentAttribute = documentAttribute;
        }

        internal RedisQueryProvider(IRedisConnection connection, DocumentAttribute documentAttribute)
        {
            Connection = connection;
            DocumentAttribute = documentAttribute;
            StateManager = new RedisCollectionStateManager(DocumentAttribute);
        }
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type;
            try
            {
                return
                   (IQueryable)Activator.CreateInstance(typeof(RedisCollection<>).
                          MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private static bool IsAggregation(MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Apply":
                case "Sum":
                case "Average":
                case "GroupBy":
                case "Min":
                case "Max":
                case "Count":
                    return true;
                default:
                    if (expression.Arguments[0] is MethodCallExpression innerExpr)
                        return (IsAggregation(innerExpr));
                    else
                        return false;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            where TElement : notnull
        {            
            return new RedisCollection<TElement>(this, expression, StateManager);
        }

        public object? Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public SearchResponse<T> ExecuteQuery<T>(Expression expression) where T : notnull
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<DocumentAttribute>();
            if(attr == null)
            {
                type = GetRootType((MethodCallExpression)expression);
                attr = type.GetCustomAttribute<DocumentAttribute>();
            }
                
            if (attr == null || string.IsNullOrEmpty(attr.IndexName))
                throw new InvalidOperationException("Searches can only be perfomred on objects decorated with a RedisObjectDefinitionAttribute that specifies a particular index");
            var query = ExpressionTranslator.BuildQueryFromExpression(expression, type);
            var genericType = typeof(T);
            var response = Connection.Search<T>(query);            
            return new SearchResponse<T>(response);
        }

        public IEnumerable<AggregationResult<T>> ExecuteAggregation<T>(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var serializedArgs = aggregation.Serialize();
            var res = Connection.Execute("FT.AGGREGATE", aggregation.Serialize());
            return AggregationResult<T>.FromRedisResult(res);
        }

        public async Task<IEnumerable<AggregationResult<T>>> ExecuteAggregationAsync<T>(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var serializedArgs = aggregation.Serialize();
            var res = await Connection.ExecuteAsync("FT.AGGREGATE", aggregation.Serialize());
            return AggregationResult<T>.FromRedisResult(res);
        }


        public RedisReply ExecuteReductiveAggregation(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var serializedArgs = aggregation.Serialize();
            var res = AggregationResult.FromRedisResult(Connection.Execute("FT.AGGREGATE", aggregation.Serialize()));
            var reductionName = ((Reduction)aggregation.Predicates.Last()).ResultName;
            return res.First()[reductionName];            
        }

        public async ValueTask<RedisReply> ExecuteReductiveAggregationAsync(MethodCallExpression expression, Type underpinningType)
        {
            var aggregation = ExpressionTranslator.BuildAggregationFromExpression(expression, underpinningType);
            var serializedArgs = aggregation.Serialize();
            var res = AggregationResult.FromRedisResult(await Connection.ExecuteAsync("FT.AGGREGATE", aggregation.Serialize()));
            var reductionName = ((Reduction)aggregation.Predicates.Last()).ResultName;
            return res.First()[reductionName];
        }

        public Type GetRootType(MethodCallExpression expression)
        {
            while (expression.Arguments[0] is MethodCallExpression innerExpression)
                expression = innerExpression;
            return expression.Arguments[0].Type.GenericTypeArguments[0];
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var type = typeof(TResult);
            if(expression is MethodCallExpression methodCall)
            {
                switch (methodCall.Method.Name)
                {
                    case "FirstOrDefault":
                        return ExecuteQuery<TResult>(expression).Documents.Values.FirstOrDefault();
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
            }
            throw new NotImplementedException();
        }

        //internal void Save()
        //{
        //    var updates = new Dictionary<string, IDictionary<string, string>>();
        //    var deletions = StateManager.Snapshot?.Keys.Where(k => StateManager.Data != null && !StateManager.Data.Keys.Contains(k)) ?? new List<string>();
        //    var additions = StateManager.Data?.Keys.Where(k => StateManager.Snapshot != null && !StateManager.Snapshot.Keys.Contains(k)) ?? new List<string>();
        //    if (StateManager.Snapshot == null && StateManager.Data == null)
        //    {
        //        return;
        //    }
        //    else if (StateManager.Snapshot != null && StateManager.Data != null)
        //    {
        //        foreach (var key in StateManager.Snapshot.Keys)
        //        {
        //            if (StateManager.Data.ContainsKey(key))
        //            {
        //                var neededUpdates = new Dictionary<string, string>();
        //                var objHash = StateManager.Data[key]?.BuildHashSet() ?? new Dictionary<string, string>();
        //                foreach (var field in objHash.Keys)
        //                {
        //                    if ((StateManager.Snapshot[key].ContainsKey(field) &&
        //                        StateManager.Snapshot[key][field] != objHash[field]) ||
        //                        !StateManager.Snapshot[key].ContainsKey(field))
        //                    {
        //                        neededUpdates.Add(field, objHash[field]);
        //                    }
        //                }
        //                if(neededUpdates.Any())
        //                    updates.Add(key, neededUpdates);
        //            }
        //        }
        //        foreach (var key in additions)
        //        {
        //            updates.Add(key, StateManager.Data[key]?.BuildHashSet() ?? new Dictionary<string, string>());
        //        }
        //    }
        //    Update(updates, deletions);
        //}

        internal void Update(IDictionary<string,IDictionary<string,string>> updates, IEnumerable<string>? deletions = null)
        {
            foreach(var item in updates)
            {
                Connection.HSet(item.Key, item.Value.ToArray());
            }
            if (deletions != null)
            {
                foreach (var item in deletions)
                {
                    Connection.Unlink(item);
                }
            }            
        }
    }
}
