using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Redis.OM.Aggregation;
using Redis.OM.Modeling;
using Redis.OM.Searching;

namespace Redis.OM
{
    /// <summary>
    /// Extensions of the Queryable Type for RedisCollections.
    /// </summary>
    public static class SearchExtensions
    {
        /// <summary>
        /// Apply the provided expression to data in Redis.
        /// </summary>
        /// <param name="source">The Aggregation set.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <param name="alias">The alias of the result.</param>
        /// <typeparam name="T">Indexed type being applied to.</typeparam>
        /// <typeparam name="TR">Type Yielded.</typeparam>
        /// <returns>An Aggregation set.</returns>
        public static RedisAggregationSet<T> Apply<T, TR>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TR>> expression, string alias)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Apply, source, expression, alias),
                   new[] { source.Expression, Expression.Quote(expression), Expression.Constant(alias) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Filters results based off of expression.
        /// </summary>
        /// <param name="source">AggregationSet to be filtered.</param>
        /// <param name="expression">The Expression to apply as the filter.</param>
        /// <typeparam name="T">The indexed type to act on.</typeparam>
        /// <returns>An aggregation set with the expression in the pipeline.</returns>
        public static RedisAggregationSet<T> Filter<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, bool>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Filter, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Count the instances where the expression is true.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The Count.</returns>
        public static int Count<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, bool>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Count, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Initial query expression if applied first, filter expression if applied later for the items to be aggregated by redis.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The filtration expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>An Aggregation set with the expression in the pipeline.</returns>
        public static RedisAggregationSet<T> Where<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, bool>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Where, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Initial query expression for the Redis Collection.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The filtration expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A RedisCollection with the expression in the pipeline.</returns>
        public static IRedisCollection<T> Where<T>(this IRedisCollection<T> source, Expression<Func<T, bool>> expression)
            where T : notnull
        {
            var collection = (RedisCollection<T>)source;
            var combined = collection.BooleanExpression == null ? expression : collection.BooleanExpression.And(expression);

            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Where, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, combined, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Finds nearest neighbors to provided vector.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression yielding the field to search on.</param>
        /// <param name="numNeighbors">Number of neighbors to search for.</param>
        /// <param name="item">The vector or item to search on.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TKnnType">The type of the vector.</typeparam>
        /// <returns>A Redis Collection with a nearest neighbors expression attached to it.</returns>
        public static IRedisCollection<T> NearestNeighbors<T, TKnnType>(this IRedisCollection<T> source, Expression<Func<T, Vector<TKnnType>>> expression, int numNeighbors, Vector<TKnnType> item)
            where T : notnull
            where TKnnType : class
        {
            var collection = (RedisCollection<T>)source;
            var booleanExpression = collection.BooleanExpression;

            var exp = Expression.Call(
                null,
                GetMethodInfo(NearestNeighbors, source, expression, numNeighbors, item),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(numNeighbors), Expression.Constant(item) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, booleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Finds nearest neighbors to provided vector.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression yielding the field to search on.</param>
        /// <param name="numNeighbors">Number of neighbors to search for.</param>
        /// <param name="item">The vector or item to search on.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TKnnType">The type of the vector.</typeparam>
        /// <returns>A Redis Collection with a nearest neighbors expression attached to it.</returns>
        public static IRedisCollection<T> NearestNeighbors<T, TKnnType>(this IRedisCollection<T> source, Expression<Func<T, Vector<TKnnType>>> expression, int numNeighbors, TKnnType item)
            where T : notnull
            where TKnnType : class
        {
            var collection = (RedisCollection<T>)source;
            var booleanExpression = collection.BooleanExpression;

            var vector = Vector.Of(item);
            var exp = Expression.Call(
                null,
                GetMethodInfo(NearestNeighbors, source, expression, numNeighbors, vector),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(numNeighbors), Expression.Constant(vector) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, booleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Specifies which items to pull out of Redis.
        /// </summary>
        /// <param name="source">The Redis Collection.</param>
        /// <param name="expression">The expression for creating the item.</param>
        /// <typeparam name="T">The indexed type built on.</typeparam>
        /// <typeparam name="TR">The type returned.</typeparam>
        /// <returns>A redis collection with the expression applied.</returns>
        public static IRedisCollection<TR> Select<T, TR>(this IRedisCollection<T> source, Expression<Func<T, TR>> expression)
            where T : notnull
            where TR : notnull
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Select, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            var res = new RedisCollection<TR>((RedisQueryProvider)source.Provider, exp, source.StateManager, null, source.SaveState, source.ChunkSize);
            res.RootType = typeof(T);
            return res;
        }

        /// <summary>
        /// Skips into the collection by the specified amount.
        /// </summary>
        /// <param name="source">The Redis Collection.</param>
        /// <param name="count">The number of items to skip.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A RedisCollection with the skip expression applied.</returns>
        public static IRedisCollection<T> Skip<T>(this IRedisCollection<T> source, int count)
            where T : notnull
        {
            var collection = (RedisCollection<T>)source;
            var exp = Expression.Call(
                null,
                GetMethodInfo(Skip, source, count),
                new[] { source.Expression, Expression.Constant(count) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, collection.BooleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Specifies the number of records to retrieve from Redis.
        /// </summary>
        /// <param name="source">The RedisCollection.</param>
        /// <param name="count">The number of Items to retrieve.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A RedisCollection with the expression applied.</returns>
        public static IRedisCollection<T> Take<T>(this IRedisCollection<T> source, int count)
            where T : notnull
        {
            var collection = (RedisCollection<T>)source;
            var exp = Expression.Call(
                null,
                GetMethodInfo(Take, source, count),
                new[] { source.Expression, Expression.Constant(count) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, collection.BooleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Counts distinct elements matching the expression.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to count.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TCount">The result type to count.</typeparam>
        /// <returns>The count of distinct elements.</returns>
        public static long CountDistinct<T, TCount>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TCount>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinct, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Approximate count of distinct elements matching the expression.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to count.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TCount">The result type to count.</typeparam>
        /// <returns>The count of distinct elements.</returns>
        public static async ValueTask<long> CountDistinctAsync<T, TCount>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TCount>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<long>(exp, typeof(T));
        }

        /// <summary>
        /// Get's a count of the members of an aggregation group.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The counts of the members within all the groups.</returns>
        public static GroupedAggregationSet<T> CountGroupMembers<T>(this GroupedAggregationSet<T> source)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountGroupMembers, source),
                new[] { source.Expression });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Counts distinct elements matching the expression.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to count.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TCount">The result type to count.</typeparam>
        /// <returns>The count of distinct elements.</returns>
        public static GroupedAggregationSet<T> CountDistinct<T, TCount>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TCount>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinct, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Approximate count of distinct elements matching the expression.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to count.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TCount">The result type to count.</typeparam>
        /// <returns>The count of distinct elements.</returns>
        public static GroupedAggregationSet<T> CountDistinctish<T, TCount>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TCount>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctish, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Calculates the standard deviation of records of the given field.
        /// </summary>
        /// <param name="source">The Aggregation Set.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <typeparam name="T">The indexed Type.</typeparam>
        /// <typeparam name="TReduce">The type to reduce.</typeparam>
        /// <returns>The standard deviation.</returns>
        public static double StandardDeviation<T, TReduce>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TReduce>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(StandardDeviation, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Calculates the standard deviation of records of the given field.
        /// </summary>
        /// <param name="source">The Aggregation Set.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <typeparam name="T">The indexed Type.</typeparam>
        /// <typeparam name="TReduce">The type to reduce.</typeparam>
        /// <returns>The standard deviation.</returns>
        public static async ValueTask<double> StandardDeviationAsync<T, TReduce>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TReduce>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(StandardDeviationAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double>(exp, typeof(T));
        }

        /// <summary>
        /// Gets distinct element for the given field.
        /// </summary>
        /// <param name="source">Aggregation set.</param>
        /// <param name="expression">The expression containing the field to get distinct fields for.</param>
        /// <typeparam name="T">The indexed Type.</typeparam>
        /// <typeparam name="TResult">The type that you are retrieving.</typeparam>
        /// <returns>An AggregationSet with the expression in the pipeline.</returns>
        public static RedisAggregationSet<T> Distinct<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Distinct, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Gets distinct element for the given field.
        /// </summary>
        /// <param name="source">Aggregation set.</param>
        /// <param name="expression">The expression containing the field to get distinct fields for.</param>
        /// <typeparam name="T">The indexed Type.</typeparam>
        /// <typeparam name="TResult">The type that you are retrieving.</typeparam>
        /// <returns>An AggregationSet with the expression in the pipeline.</returns>
        public static GroupedAggregationSet<T> Distinct<T, TResult>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Distinct, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Retrieve the first value matching the expression in Redis.
        /// </summary>
        /// <param name="source">The RedisAggregationSet.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The First Value.</returns>
        public static RedisReply FirstValue<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Retrieve the first value matching the expression in Redis.
        /// </summary>
        /// <param name="source">The RedisAggregationSet.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The First Value.</returns>
        public static async ValueTask<RedisReply> FirstValueAsync<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<TResult>(exp, typeof(T));
        }

        /// <summary>
        /// Retrieve the first value matching the expression in Redis.
        /// </summary>
        /// <param name="source">The RedisAggregationSet.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The First Value.</returns>
        public static GroupedAggregationSet<T> FirstValue<T, TResult>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });

            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Retrieve the first value matching the expression in Redis.
        /// </summary>
        /// <param name="source">The RedisAggregationSet.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <param name="sortedBy">parameter to sort by.</param>
        /// <param name="direction">direction to sor.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The First Value.</returns>
        public static RedisReply FirstValue<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, string sortedBy, SortDirection direction)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression, sortedBy, direction),
                source.Expression,
                Expression.Quote(expression),
                Expression.Constant(sortedBy),
                Expression.Constant(direction));
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Retrieve the first value matching the expression in Redis.
        /// </summary>
        /// <param name="source">The RedisAggregationSet.</param>
        /// <param name="expression">The expression to apply.</param>
        /// <param name="sortedBy">Direction to sort results by.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The First Value.</returns>
        public static RedisReply FirstValue<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, string sortedBy)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression, sortedBy),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(sortedBy) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Get the first value from redis matching the expression.
        /// </summary>
        /// <param name="source">the RedisAggregationSet.</param>
        /// <param name="expression">The expression To match.</param>
        /// <param name="sortedBy">The field to sort the records in Redis by.</param>
        /// <param name="direction">The direction to sort the records.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type of the expression.</typeparam>
        /// <returns>A redis reply containing the result.</returns>
        public static async ValueTask<RedisReply> FirstValueAsync<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, string sortedBy, SortDirection direction)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValueAsync, source, expression, sortedBy, direction),
                source.Expression,
                Expression.Quote(expression),
                Expression.Constant(sortedBy),
                Expression.Constant(direction));
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<TResult>(exp, typeof(T));
        }

        /// <summary>
        /// Get the first value from redis matching the expression.
        /// </summary>
        /// <param name="source">the RedisAggregationSet.</param>
        /// <param name="expression">The expression To match.</param>
        /// <param name="sortedBy">The field to sort the records in Redis by.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type of the expression.</typeparam>
        /// <returns>A redis reply containing the result.</returns>
        public static async ValueTask<RedisReply> FirstValueAsync<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, string sortedBy)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValueAsync, source, expression, sortedBy),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(sortedBy) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<TResult>(exp, typeof(T));
        }

        /// <summary>
        /// Gets a random sample from Redis.
        /// </summary>
        /// <param name="source">The Source.</param>
        /// <param name="expression">The expression containing the field to get the random sample for.</param>
        /// <param name="sampleSize">Random sample size.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>Aggregation set with the random sample in the pipeline.</returns>
        public static RedisAggregationSet<T> RandomSample<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, long sampleSize)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(RandomSample, source, expression, sampleSize),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(sampleSize) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Applies a geofilter.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to apply the geofilter to.</param>
        /// <param name="lon">longitude.</param>
        /// <param name="lat">latitude.</param>
        /// <param name="radius">radius.</param>
        /// <param name="unit">distance unit.</param>
        /// <typeparam name="T">IndexedType.</typeparam>
        /// <returns>A RedisCollection with the geofilter applied.</returns>
        public static IRedisCollection<T> GeoFilter<T>(this IRedisCollection<T> source, Expression<Func<T, GeoLoc?>> expression, double lon, double lat, double radius, GeoLocDistanceUnit unit)
            where T : notnull
        {
            var collection = (RedisCollection<T>)source;
            var exp = Expression.Call(
                null,
                GetMethodInfo(GeoFilter, source, expression, lon, lat, radius, unit),
                source.Expression,
                Expression.Quote(expression),
                Expression.Constant(lon),
                Expression.Constant(lat),
                Expression.Constant(radius),
                Expression.Constant(unit));
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, collection.BooleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Orders the collection by the provided attribute.
        /// </summary>
        /// <param name="source">The Redis Collection.</param>
        /// <param name="expression">The expression.</param>
        /// <typeparam name="T">The base type.</typeparam>
        /// <typeparam name="TField">The field type to order by.</typeparam>
        /// <returns>A redis collection extending the pipeline of linq expressions with the relevant SORTBY.</returns>
        public static IRedisCollection<T> OrderBy<T, TField>(this IRedisCollection<T> source, Expression<Func<T, TField>> expression)
            where T : notnull
        {
            var collection = (RedisCollection<T>)source;
            var exp = Expression.Call(
                null,
                GetMethodInfo(OrderBy, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, collection.BooleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Orders the collection by the provided attribute.
        /// </summary>
        /// <param name="source">The Redis Collection.</param>
        /// <param name="expression">The expression.</param>
        /// <typeparam name="T">The base type.</typeparam>
        /// <typeparam name="TField">The field type to order by.</typeparam>
        /// <returns>A redis collection extending the pipeline of linq expressions with the relevant SORTBY.</returns>
        public static IRedisCollection<T> OrderByDescending<T, TField>(this IRedisCollection<T> source, Expression<Func<T, TField>> expression)
            where T : notnull
        {
            var collection = (RedisCollection<T>)source;
            var exp = Expression.Call(
                null,
                GetMethodInfo(OrderByDescending, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager, collection.BooleanExpression, source.SaveState, source.ChunkSize);
        }

        /// <summary>
        /// Get a Random sample from redis.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <param name="sampleSize">The sample size.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The result to get the sample for.</typeparam>
        /// <returns>A grouped aggregation set with the expression in it's pipeline..</returns>
        public static GroupedAggregationSet<T> RandomSample<T, TResult>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, long sampleSize)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(RandomSample, source, expression, sampleSize),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(sampleSize) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Retrieve the record at the given quantile. e.g. quantile .5 is median.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression to get the quantile for.</param>
        /// <param name="quantile">The quantile.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The field type.</typeparam>
        /// <returns>The item at the given quantile.</returns>
        public static double Quantile<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, double quantile)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Quantile, source, expression, quantile),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(quantile) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Retrieve the record at the given quantile. e.g. quantile .5 is median.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression to get the quantile for.</param>
        /// <param name="quantile">The quantile.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TResult">The field type.</typeparam>
        /// <returns>The item at the given quantile.</returns>
        public static async ValueTask<double> QuantileAsync<T, TResult>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TResult>> expression, double quantile)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Quantile, source, expression, quantile),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(quantile) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<TResult>(exp, typeof(T));
        }

        /// <summary>
        /// Retrieve the record at the given quantile. e.g. quantile .5 is median.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression to get the quantile for.</param>
        /// <param name="quantile">The quantile.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <returns>The item at the given quantile.</returns>
        public static GroupedAggregationSet<T> Quantile<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression, double quantile)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Quantile, source, expression, quantile),
                new[] { source.Expression, Expression.Quote(expression), Expression.Constant(quantile) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Retrieves an approximate distinct count of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The type the field is being taken from.</typeparam>
        /// <returns>An approximate count.</returns>
        public static long CountDistinctish<T, TField>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctish, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        /// <summary>
        /// Retrieves an approximate distinct count of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The type the field is being taken from.</typeparam>
        /// <returns>An approximate count.</returns>
        public static async ValueTask<long> CountDistinctishAsync<T, TField>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctish, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<long>(exp, typeof(T));
        }

        /// <summary>
        /// Loads the provided property or properties regardless of whether or not they are set up as Aggregatable in Redis.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression to use for the load.</param>
        /// <typeparam name="T">The base type.</typeparam>
        /// <typeparam name="TLoadType">The Type to instruct redis to load.</typeparam>
        /// <returns>A RedisAggregationSet.</returns>
        public static RedisAggregationSet<T> Load<T, TLoadType>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TLoadType>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Load, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Loads all indexed attributes in a document into the Aggregation pipeline.
        /// </summary>
        /// <param name="source">The source set.</param>
        /// <typeparam name="T">The base type.</typeparam>
        /// <returns>A RedisAggregationSet.</returns>
        public static RedisAggregationSet<T> LoadAll<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(LoadAll, source),
                source.Expression);
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Loads the provided property or properties regardless of whether or not they are set up as Aggregatable in Redis.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression to use for the load.</param>
        /// <typeparam name="T">The base type.</typeparam>
        /// <typeparam name="TLoadType">The Type to instruct redis to load.</typeparam>
        /// <returns>A GroupedAggregationSet.</returns>
        public static GroupedAggregationSet<T> Load<T, TLoadType>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TLoadType>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Load, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Loads all indexed attributes in a document into the Aggregation pipeline.
        /// </summary>
        /// <param name="source">The source set.</param>
        /// <typeparam name="T">The base type.</typeparam>
        /// <returns>A RedisAggregationSet.</returns>
        public static GroupedAggregationSet<T> LoadAll<T>(this GroupedAggregationSet<T> source)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(LoadAll, source),
                source.Expression);
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Group like records together by provided fields.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TGroupType">The group type.</typeparam>
        /// <returns>A GroupedAggregationSet.</returns>
        public static GroupedAggregationSet<T> GroupBy<T, TGroupType>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TGroupType>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(GroupBy, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Group like records together by provided fields.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TGroupType">The group type.</typeparam>
        /// <returns>A GroupedAggregationSet.</returns>
        public static GroupedAggregationSet<T> GroupBy<T, TGroupType>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TGroupType>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(GroupBy, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Reduce the average of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The field type to average.</typeparam>
        /// <returns>GroupedAggregationSet with the reducer in the pipeline.</returns>
        public static GroupedAggregationSet<T> Average<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Average, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Reduce the Sum of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The field type to reduce.</typeparam>
        /// <returns>GroupedAggregationSet with the reducer in the pipeline.</returns>
        public static GroupedAggregationSet<T> Sum<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Sum, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Reduce the Min of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The field type to reduce.</typeparam>
        /// <returns>GroupedAggregationSet with the reducer in the pipeline.</returns>
        public static GroupedAggregationSet<T> Min<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Min, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Reduce the Max of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The field type to reduce.</typeparam>
        /// <returns>GroupedAggregationSet with the reducer in the pipeline.</returns>
        public static GroupedAggregationSet<T> Max<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Max, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Reduce the Standard Deviation of the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The field expression.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <typeparam name="TField">The field type to reduce.</typeparam>
        /// <returns>GroupedAggregationSet with the reducer in the pipeline.</returns>
        public static GroupedAggregationSet<T> StandardDeviation<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(StandardDeviation, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Order the results by the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to order by.</param>
        /// <typeparam name="T">The Indexed type.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <returns>A set with the expression applied.</returns>
        public static GroupedAggregationSet<T> OrderBy<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderBy, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Order the results by the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to order by.</param>
        /// <typeparam name="T">The Indexed type.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <returns>A set with the expression applied.</returns>
        public static GroupedAggregationSet<T> OrderByDescending<T, TField>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderByDescending, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Order the results by the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to order by.</param>
        /// <typeparam name="T">The Indexed type.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <returns>A set with the expression applied.</returns>
        public static RedisAggregationSet<T> OrderBy<T, TField>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderBy, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Order the results by the provided field.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression to order by.</param>
        /// <typeparam name="T">The Indexed type.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <returns>A set with the expression applied.</returns>
        public static RedisAggregationSet<T> OrderByDescending<T, TField>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, TField>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderByDescending, source, expression),
                   new[] { source.Expression, Expression.Quote(expression) });
            return new RedisAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Closes out the group and yields a regular RedisAggregationSet. Use this to flush reductions further
        /// down the pipeline.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A RedisAggregationSet with the current pipeline preserved.</returns>
        public static RedisAggregationSet<T> CloseGroup<T>(this GroupedAggregationSet<T> source)
        {
            return new RedisAggregationSet<T>(source, source.Expression);
        }

        /// <summary>
        /// Get's the first element from Redis.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A single result.</returns>
        public static AggregationResult<T>? FirstOrDefault<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(FirstOrDefault, source),
                   source.Expression);
            return ((RedisQueryProvider)source.Provider).ExecuteAggregation<T>(exp, typeof(T)).FirstOrDefault();
        }

        /// <summary>
        /// Get's the first element from Redis.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A single result.</returns>
        public static async ValueTask<AggregationResult<T>?> FirstOrDefaultAsync<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(FirstOrDefault, source),
                   source.Expression);
            return (await ((RedisQueryProvider)source.Provider).ExecuteAggregationAsync<T>(exp, typeof(T))).FirstOrDefault();
        }

        /// <summary>
        /// Get's the first element from Redis.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A single result.</returns>
        public static async ValueTask<AggregationResult<T>> FirstAsync<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(FirstOrDefault, source),
                   source.Expression);
            return (await ((RedisQueryProvider)source.Provider).ExecuteAggregationAsync<T>(exp, typeof(T))).First();
        }

        /// <summary>
        /// Get's the first element from Redis.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>A single result.</returns>
        public static AggregationResult<T> First<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(First, source),
                   source.Expression);
            return ((RedisQueryProvider)source.Provider).ExecuteAggregation<T>(exp, typeof(T)).First();
        }

        /// <summary>
        /// Skip a certain number of elements in Redis before reading results back.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="count">The number to skip.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The results.</returns>
        public static GroupedAggregationSet<T> Skip<T>(this GroupedAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Skip, source, count),
                   new[] { source.Expression, Expression.Constant(count) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Take only a certain number of elements from Redis.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="count">The number to skip.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The results.</returns>
        public static GroupedAggregationSet<T> Take<T>(this GroupedAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Take, source, count),
                   new[] { source.Expression, Expression.Constant(count) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Skip a certain number of elements in Redis before reading results back.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="count">The number to skip.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The results.</returns>
        public static GroupedAggregationSet<T> Skip<T>(this RedisAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Skip, source, count),
                   new[] { source.Expression, Expression.Constant(count) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Take only a certain number of elements from Redis.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="count">The number to skip.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The results.</returns>
        public static GroupedAggregationSet<T> Take<T>(this RedisAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Take, source, count),
                   new[] { source.Expression, Expression.Constant(count) });
            return new GroupedAggregationSet<T>(source, exp);
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<double> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<int> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<int>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<long> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<long>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<float> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<float>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<decimal> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<decimal>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<double?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<int?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<int?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<long?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<long?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<float?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<float?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs sum reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to sum.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The sum.</returns>
        public static async ValueTask<decimal?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<decimal?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<double> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<double> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<double> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<float> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<float>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<decimal> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<decimal>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<double?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<double?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<int?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<int?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<long?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<long?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<float?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<float?>(exp, typeof(T));
        }

        /// <summary>
        /// Runs average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to average.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The average.</returns>
        public static async ValueTask<decimal?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<decimal?>(exp, typeof(T));
        }

        /// <summary>
        /// Max average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to max.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The max.</returns>
        public static async ValueTask<RedisReply> MaxAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, RedisReply>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(MaxAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<int>(exp, typeof(T));
        }

        /// <summary>
        /// Min average reduction.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <param name="expression">The field expression to min.</param>
        /// <typeparam name="T">The indexed type.</typeparam>
        /// <returns>The min.</returns>
        public static async ValueTask<RedisReply> MinAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, RedisReply>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(MinAsync, source, expression),
                new[] { source.Expression, Expression.Quote(expression) });
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync<T>(exp, typeof(T));
        }

        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f)
        {
            return f.Method;
        }

        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused)
        {
            return f.Method;
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
        {
            return f.Method;
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
        {
            return f.Method;
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
        {
            return f.Method;
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
        {
            return f.Method;
        }
    }
}
