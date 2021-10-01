using NRedisPlus.RediSearch;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public static class QueryableExtensions
    {
        public static MethodInfo GetMethodInfo<t1, t2>(Func<t1, t2> f)
        {
            return f.Method;
        }
        public static MethodInfo GetMethodInfo<t1,t2>(Func<t1,t2> f, t1 unused)
        {
            return f.Method;
        }
        public static MethodInfo GetMethodInfo<t1, t2, t3>(Func<t1, t2, t3> f, t1 unused1, t2 unused2)
        {
            return f.Method;
        }

        public static MethodInfo GetMethodInfo<t1, t2, t3, t4>(Func<t1, t2, t3, t4> f, t1 unused1, t2 unused2, t3 unused3)
        {
            return f.Method;
        }

        public static MethodInfo GetMethodInfo<t1, t2, t3, t4, t5>(Func<t1, t2, t3, t4, t5> f, t1 unused1, t2 unused2, t3 unused3, t4 unused4)
        {
            return f.Method;
        }

        public static MethodInfo GetMethodInfo<t1, t2, t3, t4, t5, t6, t7>(Func<t1, t2, t3, t4, t5, t6, t7> f, t1 unused1, t2 unused2, t3 unused3, t4 unused4, t5 unused5, t6 unused6)
        {
            return f.Method;
        }

        public static RedisAggregationSet<T> Apply<T,R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, string alias)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Apply, source, expression, alias),
                   new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(alias) }
                   );
            return new RedisAggregationSet<T>(source, exp);
        }

        public static RedisAggregationSet<T> Filter<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, bool>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Filter, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression)}
                   );
            return new RedisAggregationSet<T>(source, exp);
        }

        public static int Count<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, bool>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Count, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }
        public static RedisAggregationSet<T> Where<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, bool>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Where, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new RedisAggregationSet<T>(source, exp);
        }

        public static RedisCollection<T> Where<T>(this RedisCollection<T> source, Expression<Func<T, bool>> expression) where T : notnull
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Where, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager);
        }

        public static RedisCollection<T> Select<T>(this RedisCollection<T> source, Expression<Func<T, bool>> expression) where T : notnull
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Select, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager);
        }

        public static long CountDistinct<T,R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinct, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static async ValueTask<long> CountDistinctAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static GroupedAggregationSet<T> CountDistinct<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinct, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> CountDistinctish<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctish, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static double StandardDeviation<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(StandardDeviation, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static async ValueTask<double> StandardDeviationAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(StandardDeviationAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static RedisAggregationSet<T> Distinct<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Distinct, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return new RedisAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Distinct<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Distinct, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static RedisReply FirstValue<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static async ValueTask<RedisReply> FirstValueAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static GroupedAggregationSet<T> FirstValue<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static RedisReply FirstValue<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, string sortedBy, SortDirection direciton)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression, sortedBy, direciton),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(sortedBy), Expression.Constant(direciton) }
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static RedisReply FirstValue<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, string sortedBy)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValue, source, expression, sortedBy),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(sortedBy)}
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static async ValueTask<RedisReply> FirstValueAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, string sortedBy, SortDirection direciton)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValueAsync, source, expression, sortedBy, direciton),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(sortedBy), Expression.Constant(direciton) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<RedisReply> FirstValueAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, string sortedBy)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(FirstValueAsync, source, expression, sortedBy),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(sortedBy) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static RedisAggregationSet<T> RandomSample<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, long sampleSize)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(RandomSample, source, expression, sampleSize),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(sampleSize) }
                );
            return new RedisAggregationSet<T>(source, exp);            
        }

        public static RedisCollection<T> GeoFilter<T,R>(this RedisCollection<T> source, Expression<Func<T, R>> expression, double lon, double lat, double radius, GeoLocDistanceUnit unit) where T : notnull
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(GeoFilter, source, expression, lon, lat, radius, unit),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(lon), Expression.Constant(lat), Expression.Constant(radius), Expression.Constant(unit) }
                );
            return new RedisCollection<T>((RedisQueryProvider)source.Provider, exp, source.StateManager);
        }

        public static GroupedAggregationSet<T> RandomSample<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, long sampleSize)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(RandomSample, source, expression, sampleSize),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(sampleSize) }
                );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static double Quantile<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, double quantile)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Quantile, source, expression, quantile),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(quantile) }
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static async ValueTask<double> QuantileAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, double quantile)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Quantile, source, expression, quantile),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(quantile) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static GroupedAggregationSet<T> Quantile<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression, double quantile)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(Quantile, source, expression, quantile),
                new Expression[] { source.Expression, Expression.Quote(expression), Expression.Constant(quantile) }
                );
            return new GroupedAggregationSet<T>(source, exp);            
        }

        public static long CountDistinctish<T,R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>,R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctish, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregation(exp, typeof(T));
        }

        public static async ValueTask<long> CountDistinctishAsync<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(CountDistinctish, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static GroupedAggregationSet<T> GroupBy<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(GroupBy, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression)}
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> GroupBy<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(GroupBy, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Average<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Average, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Sum<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Sum, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Min<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Min, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Max<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Max, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> StandardDeviation<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(StandardDeviation, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> OrderBy<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderBy, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> OrderByDescending<T, R>(this GroupedAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderByDescending, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static RedisAggregationSet<T> OrderBy<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderBy, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new RedisAggregationSet<T>(source, exp);
        }

        public static RedisAggregationSet<T> OrderByDescending<T, R>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, R>> expression)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(OrderByDescending, source, expression),
                   new Expression[] { source.Expression, Expression.Quote(expression) }
                   );
            return new RedisAggregationSet<T>(source, exp);
        }

        public static RedisAggregationSet<T> CloseGroup<T>(this GroupedAggregationSet<T> source)
        {            
            return new RedisAggregationSet<T>(source, source.Expression);
        }

        public static AggregationResult<T> FirstOrDefault<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(FirstOrDefault, source),
                   new Expression[] { source.Expression }
                   );
            return ((RedisQueryProvider)source.Provider).ExecuteAggregation<T>(exp, typeof(T)).FirstOrDefault();
        }

        public static async ValueTask<AggregationResult<T>> FirstOrDefaultAsync<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(FirstOrDefault, source),
                   new Expression[] { source.Expression }
                   );
            return (await ((RedisQueryProvider)source.Provider).ExecuteAggregationAsync<T>(exp, typeof(T))).FirstOrDefault();
        }

        public static async ValueTask<AggregationResult<T>> FirstAsync<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(FirstOrDefault, source),
                   new Expression[] { source.Expression }
                   );
            return (await ((RedisQueryProvider)source.Provider).ExecuteAggregationAsync<T>(exp, typeof(T))).First();
        }

        public static AggregationResult<T> First<T>(this RedisAggregationSet<T> source)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(First, source),
                   new Expression[] { source.Expression }
                   );
            return ((RedisQueryProvider)source.Provider).ExecuteAggregation<T>(exp, typeof(T)).First();
        }


        public static GroupedAggregationSet<T> Skip<T>(this GroupedAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Skip, source, count),
                   new Expression[] { source.Expression, Expression.Constant(count) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Take<T>(this GroupedAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Take, source, count),
                   new Expression[] { source.Expression, Expression.Constant(count) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Skip<T>(this RedisAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Skip, source, count),
                   new Expression[] { source.Expression, Expression.Constant(count) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }

        public static GroupedAggregationSet<T> Take<T>(this RedisAggregationSet<T> source, int count)
        {
            var exp = Expression.Call(
                   null,
                   GetMethodInfo(Take, source, count),
                   new Expression[] { source.Expression, Expression.Constant(count) }
                   );
            return new GroupedAggregationSet<T>(source, exp);
        }
        #region async overrides
        public static async ValueTask<double> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<int> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<long> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<float> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<decimal> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<double?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<int?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<long?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<float?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<decimal?> SumAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(SumAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }


        public static async ValueTask<double> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<double> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<double> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<float> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<decimal> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<double?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, double?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<int?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, int?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<long?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, long?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<float?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, float?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<decimal?> AverageAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, decimal?>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(AverageAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<RedisReply> MaxAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, RedisReply>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(MaxAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }

        public static async ValueTask<RedisReply> MinAsync<T>(this RedisAggregationSet<T> source, Expression<Func<AggregationResult<T>, RedisReply>> expression)
        {
            var exp = Expression.Call(
                null,
                GetMethodInfo(MinAsync, source, expression),
                new Expression[] { source.Expression, Expression.Quote(expression) }
                );
            return await ((RedisQueryProvider)source.Provider).ExecuteReductiveAggregationAsync(exp, typeof(T));
        }
        #endregion
    }
}
