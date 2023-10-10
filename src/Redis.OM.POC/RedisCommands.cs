using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Redis.OM.Contracts;

namespace Redis.OM
{
    public static class RedisCommands
    {
        public static string Ping(this IRedisConnection connection) => connection.Execute("PING");

        public static string Set(this IRedisConnection connection, string key, string value) => connection.Execute("SET", key, value);

        public static string Get(this IRedisConnection connection, string key) => connection.Execute("GET", key);
        #region list operations
        public static string LIndex(this IRedisConnection connection, string key, long index) => connection.Execute("LINDEX", key, index.ToString());

        public static long? LPush(this IRedisConnection connection, string key, params string[] values)
        {
            var args = new List<string>();
            args.Add(key);
            args.AddRange(values);
            return connection.Execute("LPUSH", args.ToArray());
        }
        public static long RPush(this IRedisConnection connection, string key, params string[] values)
        {
            var args = new List<string>();
            args.Add(key);
            args.AddRange(values);
            return (long)connection.Execute("RPUSH", args.ToArray());            
        }

        public static string LSet(this IRedisConnection connection, string key, int index, string value) => connection.Execute("LSet", key, index.ToString(), value);

        public static long? LLen(this IRedisConnection connection, string key) => connection.Execute("LLEN", key);

        public static int LPos(this IRedisConnection connection, string key, string element)
        {
            var reply = connection.Execute("LPOS", key, element);             
            if (reply == null)
            {
                return -1;
            }
            else
            {
                return reply;
            }
        }
        public static string[] LRange(this IRedisConnection connection, string key, long start, long stop) => connection.Execute("LRANGE", key, start.ToString(), stop.ToString());

        public static long LRem(this IRedisConnection connection, string key, string value, int count = 1) => (long)connection.Execute("LREM", key, value, count.ToString());
        #endregion

        #region Hash Operations
        public static string[] HMGet(this IRedisConnection connection, string key, params string[] fields)
        {
            var args = new List<string>();
            args.Add(key);
            args.AddRange(fields);
            return connection.Execute("HGET", args.ToArray());            
        }


        public static long? HLen(this IRedisConnection connection, string key)=> connection.Execute("HLEN", key);

        public static List<KeyValuePair<string, string>> HScan(this IRedisConnection connection, string key, ref int cursor, string match = "", uint count = 100)
        {
            var ret = new List<KeyValuePair<string, string>>();
            var args = new List<string> { key, cursor.ToString(), "COUNT", count.ToString() };
            if (!string.IsNullOrEmpty(match))
            {
                args.Add("MATCH");
                args.Add(match);
            }
            var res = connection.Execute("HSCAN", args.ToArray()).ToArray();            
            var resultsArr = res[1].ToArray();
            cursor = int.Parse(res[0]);
            for (var i = 0; i < resultsArr.Length; i+=2)
            {
                ret.Add(new KeyValuePair<string, string>(resultsArr[i], resultsArr[i + 1]));
            }
            return ret;
        }

        public static bool HDel(this IRedisConnection connection, string key, string field) => connection.Execute("HDEL", key, field) == 1;
        #endregion
        public static string Del(this IRedisConnection connection, string key) => connection.Execute("DEL", key); 

        

        #region Set Operations
        public static long SAdd(this IRedisConnection connection, string key, params string[] members)
        {
            var args = new List<string>();
            args.Add(key);
            args.AddRange(members);
            return (long)connection.Execute("SADD", args.ToArray());            
        }

        public static long SCard(this IRedisConnection connection, string key) => (long)connection.Execute("SCARD", key);

        public static string[] SDiff(this IRedisConnection connection, params string[] keys) => connection.Execute("SDIFF", keys);

        public static long SDiffStore(this IRedisConnection connection, string destination, params string[] keys) => (long)connection.Execute("SDIFFSTORE", keys.Prepend(destination).ToArray());        

        public static string[] SInter(this IRedisConnection connection, params string[] keys)
        {
            throw new NotImplementedException();
        }

        public static long SInterStore(this IRedisConnection connection, string destination, params string[] keys)
        {
            throw new NotImplementedException();
        }

        public static bool SIsMember(this IRedisConnection connection, string key, string member) => connection.Execute("SISMEMBER", key, member) == 1;

        public static string[] SMembers(this IRedisConnection connection, string key) => connection.Execute("SMEMBERS", key);

        public static bool[] SMIsMember(this IRedisConnection connection, string key, params string[] members) => connection.Execute("SMISMEMBER", new[] { key }.Concat(members).ToArray()).ToArray().Select(i => i == 1).ToArray();

        public static bool SMove(this IRedisConnection connection, string source, string destination, string member) => connection.Execute("SMOVE", source, destination, member) == 1;

        public static string[] SPop(this IRedisConnection connection, string key, long count = 1) => connection.Execute("SPOP", key, count.ToString());

        public static string[] SRandMember(this IRedisConnection connection, string key, long count = 1) => connection.Execute("SRANDMEMBER", key, count.ToString());

        public static long SRem(this IRedisConnection connection, string key, params string[] members)
        {
            return connection.Execute("SREM", new[] { key }.Concat(members).ToArray());
        }

        public static string[] SScan(this IRedisConnection connection, string key, ref int cursor, string? match = null, uint count = 100)
        {
            var args = new[] { key, cursor.ToString(), "Count", count.ToString() };
            if (!string.IsNullOrEmpty(match))
            {
                args.Append("MATCH");
                args.Append(match);
            }

            var res = connection.Execute("SSCAN", args);            
            cursor = int.Parse(res.ToArray()[0]);
            return res.ToArray()[1];
        }

        public static string[] SUnion(this IRedisConnection connection, params string[] keys) => connection.Execute("SUNION", keys);

        public static long SUnionStore(this IRedisConnection connection, string destination, params string[] keys) => connection.Execute("SUNIONSTORE", keys.Prepend(destination).ToArray());        
        #endregion
        #region Sorted Set Operations
        public static long ZAdd(this IRedisConnection connection, string key, params SortedSetEntry[] members)
        {
            var args = SortedSetEntry.BuildRequestArray(members).Prepend(key).ToArray();
            return connection.Execute("ZADD", args);            
        }

        public static long ZCard(this IRedisConnection connection, string key) => connection.Execute("ZCARD", key);

        public static long ZCount(this IRedisConnection connection, string key, double min, double max) => connection.Execute("ZCOUNT", key, min.ToString(), max.ToString());
        public static SortedSetEntry[] ZDiffWithScores(this IRedisConnection connection, params string[] keys)
        {
            var args = keys.Prepend(keys.Length.ToString()).Append("WITHSCORES").ToArray();
            throw new NotImplementedException();
        }
        public static string[] ZDiff(this IRedisConnection connection, params string[] keys) => connection.Execute("ZDIFF", keys.Prepend(keys.Length.ToString()).ToArray());        

        public static long ZDiffStore(this IRedisConnection connection, string destination, params string[] keys) => connection.Execute("ZDIFFSTORE", keys.Prepend(destination).ToArray());

        public static double? ZIncrBy(this IRedisConnection connection, string key, double increment, string member) => connection.Execute("ZINCRBY", key, increment.ToString(), member);

        private static List<string> BuildSortedSetOperationsArgs(string[] keys, double[]? weights = null, string aggregate = "")
        {
            var args = new List<string>(keys.Prepend(keys.Length.ToString()));
            if (weights != null)
            {
                args.Append("WEIGHTS");
                args.AddRange(weights.Select(w => w.ToString()));
            }
            if (!string.IsNullOrEmpty(aggregate))
            {
                args.Add("AGGREGATE");
                args.Add(aggregate);
            }
            return args;
        }

        private static List<string> ComposeRangeArgs(double? min, double? max, bool minInclusive = false, bool maxInclusive = false, int offset = 0, int count = -1)
        {
            var composedArgs = new List<string>();
            if(min == null)
            {
                composedArgs.Add("-inf");
            }
            else if (minInclusive)
            {
                composedArgs.Add($"[{min}");
            }
            else
            {
                composedArgs.Add($"({min}");
            }
            if (max == null)
            {
                composedArgs.Add("+inf");
            }
            else if (maxInclusive)
            {
                composedArgs.Add($"[{max}");
            }
            else
            {
                composedArgs.Add($"({max}");
            }
            if (count > -1)
            {
                composedArgs.Add("LIMIT");
                composedArgs.Add(offset.ToString());
                composedArgs.Add(count.ToString());
            }
            return composedArgs;
        }
        private static List<string> ComposeRangeArgs(string? min, string? max, bool minInclusive = false, bool maxInclusive = false, int offset = 0, int count = -1)
        {
            var composedArgs = new List<string>();
            if (min == null)
            {
                composedArgs.Add("-");
            }
            else if (minInclusive)
            {
                composedArgs.Add($"[{min}");
            }
            else
            {
                composedArgs.Add($"({min}");
            }
            if (max == null)
            {
                composedArgs.Add("+");
            }
            else if (maxInclusive)
            {
                composedArgs.Add($"[{max}");
            }
            else
            {
                composedArgs.Add($"({max}");
            }
            if (count > 0)
            {
                composedArgs.Add("LIMIT");
                composedArgs.Add(offset.ToString());
                composedArgs.Add(count.ToString());
            }
            return composedArgs;
        }

        public static SortedSetEntry[] ZInterWithScores(this IRedisConnection connection, string[] keys, double[]? weights = null, string aggregate = "")
        {
            var args = BuildSortedSetOperationsArgs(keys, weights, aggregate);
            args.Add("WITHSCORES");
            throw new NotImplementedException();
        }

        public static string[] ZInter(this IRedisConnection connection, string[] keys, double[]? weights = null, string aggregate = "")
        {
            var args = BuildSortedSetOperationsArgs(keys, weights, aggregate);
            return connection.Execute("ZINTER", args.ToArray());            
        }

        public static long ZInterStore(this IRedisConnection connection, string destination, string[] keys, double[]? weights = null, string aggregate = "")
        {
            var args = BuildSortedSetOperationsArgs(keys, weights, aggregate);
            return connection.Execute("ZINTERSTORE", args.ToArray());
        }

        public static int? ZLexCount(this IRedisConnection connection, string key, string min, string max, bool minInclusive = false, bool maxInclusive = false)
        {
            var args = new List<string>();
            args.Add(key);
            args.AddRange(ComposeRangeArgs(min, max, minInclusive, maxInclusive));            
            return connection.Execute("ZLEXCOUNT", args.ToArray());            
        }

        public static double[] ZMScore(this IRedisConnection connection, string key, params string[] members)
        {
            var args = members.Prepend(key);
            return connection.Execute("ZMSCORE", args.ToArray());            
        }

        public static SortedSetEntry[] ZPopMax(this IRedisConnection connection, string key, int count) => throw new NotImplementedException();

        public static SortedSetEntry[] ZPopMin(this IRedisConnection connection, string key, int count) => throw new NotImplementedException();

        public static SortedSetEntry[] ZRandMemberWithScores(this IRedisConnection connection, string key, int count) => throw new NotImplementedException();

        public static string?[] ZRandMembers(this IRedisConnection connection, string key, int count) => connection.Execute("ZRANDMEMBER", key, count.ToString());

        public static SortedSetEntry[] ZRangeWithScores(this IRedisConnection connection, string key, int min, int max, bool reverse = false)
        {
            var args = new List<string>();
            args.Add(key);
            args.Add(min.ToString());
            args.Add(max.ToString());
            if (reverse)
            {
                args.Add("REV");
            }
            args.Add("WITHSCORES");
            throw new NotImplementedException();        
        }

        public static string?[] ZRange(this IRedisConnection connection, string key, int min, int max, bool reverse = false)
        {
            var args = new List<string>();
            args.Add(key);
            args.Add(min.ToString());
            args.Add(max.ToString());
            if (reverse)
            {
                args.Add("REV");
            }            
            return connection.Execute("ZRANGE", args.ToArray());            
        }

        public static long ZRangeStore(this IRedisConnection connection, string destination, string key, int min, int max, bool reverse = false)
        {
            var args = new List<string>();
            args.Add(destination);
            args.Add(key);
            args.Add(min.ToString());
            args.Add(max.ToString());
            if (reverse)
            {
                args.Add("REV");
            }            
            return connection.Execute("ZRANGE", args.ToArray());            
        }

        public static long? ZRank(this IRedisConnection connection, string key, string member) => connection.Execute("ZRANK", key, member);

        public static long ZRem(this IRedisConnection connection, string key, params string[] members)
        {
            var args = members.Prepend(key);
            return connection.Execute("ZREM", args.ToArray());            
        }

        public static long ZRemRangeByLex(this IRedisConnection connection, string key, string min, string max, bool inclusiveMin = true, bool inclusiveMax = true)
        {
            var args = new List<string> { key };
            args.AddRange(ComposeRangeArgs(min, max, inclusiveMin, inclusiveMax));
            return connection.Execute("ZREMRANGEBYLEX", args.ToArray());            
        }

        public static long ZRemRangeByScore(this IRedisConnection connection, string key, double min, double max, bool inclusiveMin = true, bool inclusiveMax = true)
        {
            var args = new List<string> { key };
            args.AddRange(ComposeRangeArgs(min, max, inclusiveMin, inclusiveMax));
            return connection.Execute("ZREMRANGEBYSCORE", args.ToArray());            
        }

        public static long ZRemRangeByRank(this IRedisConnection connection, string key, int min, int max)
        {
            var args = new List<string> { key };
            return connection.Execute("ZREMRANGEBYRANK", key, min.ToString(), max.ToString());            
        }

        public static SortedSetEntry[] ZRevRangeWithScores(this IRedisConnection connection, string key, int start, int stop) => throw new NotImplementedException();

        public static string?[] ZRevRange(this IRedisConnection connection, string key, int start, int stop) => connection.Execute("ZREVRANGE", key, start.ToString(), stop.ToString());

        public static string?[] ZRevRangeByLex(this IRedisConnection connection, string key, string max, string min, bool inclusiveMin = true, bool inclusiveMax = true, int offset = 0, int count = -1)
        {
            var args = new List<string> { key };
            args.AddRange(ComposeRangeArgs(min, max, inclusiveMin, inclusiveMax,offset,count));
            return connection.Execute("ZREVRANGEBYLEX", args.ToArray());
        }

        public static SortedSetEntry[] ZRevRangeByScoreWithScores(this IRedisConnection connection, string key, double min, double max, bool inclusiveMin = true, bool inclusiveMax = true, int offset = 0, int count = -1)
        {
            var args = new List<string> { key };
            args.AddRange(ComposeRangeArgs(min, max, inclusiveMin, inclusiveMax, offset, count));
            args.Add("WITHSCORES");
            throw new NotImplementedException();            
        }

        public static string?[] ZRevRangeByScore(this IRedisConnection connection, string key, double min, double max, bool inclusiveMin = true, bool inclusiveMax = true, int offset = 0, int count = -1)
        {
            var args = new List<string> { key };
            args.AddRange(ComposeRangeArgs(min, max, inclusiveMin, inclusiveMax, offset, count));            
            return connection.Execute("ZREVRANGEBYSCORE", args.ToArray()); 
        }

        public static long? ZRevRank(this IRedisConnection connection, string key, string member) => connection.Execute("ZREVRANK", key, member);

        public static SortedSetEntry[] ZScan(this IRedisConnection connection, string key, ref int cursor, string match = "", uint count = 100)
        {
            var args = new List<string> { key, cursor.ToString(),"COUNT", count.ToString()};
            if (!string.IsNullOrEmpty(match))
            {
                args.Add("MATCH");
                args.Add(match);
            }
            var res = connection.Execute("ZSCAN", args.ToArray());             
            cursor = int.Parse(res.ToArray()[0]);
            throw new NotImplementedException();
        }

        public static double? ZScore(this IRedisConnection connection, string key, string member) => connection.Execute("ZSCORE", key, member);

        public static SortedSetEntry[] ZUnionWithScores(this IRedisConnection connection, string[] keys, double[]? weights = null, string aggregate = "")
        {
            var args = BuildSortedSetOperationsArgs(keys, weights, aggregate);
            args.Add("WITHSCORES");
            throw new NotImplementedException();       
        }

        public static string[] ZUnion(this IRedisConnection connection, string[] keys, double[]? weights, string aggregate = "")
        {
            var args = BuildSortedSetOperationsArgs(keys, weights, aggregate);            
            return connection.Execute("ZUNION", args.ToArray());            
        }

        public static long ZUnionStore(this IRedisConnection connection, string destination,string[] keys, double[]? weights, string aggregate = "")
        {
            var args = new List<string>{ destination };
            args.AddRange(BuildSortedSetOperationsArgs(keys, weights, aggregate));
            return connection.Execute("ZUNIONSTORE", args.ToArray());            
        }
        #endregion
        #region scripting
        internal static int? Eval(this IRedisConnection connection, string script, string[] keys, string[] argv)
        {
            var args = new List<string>();
            args.Add(script);
            args.Add(keys.Count().ToString());
            args.AddRange(keys);
            args.AddRange(argv);
            return connection.Execute("EVAL", args.ToArray());
        }
        
        internal static async Task<int?> EvalAsync(this IRedisConnection connection, string script, string[] keys, string[] argv)
        {
            var args = new List<string>();
            args.Add(script);
            args.Add(keys.Count().ToString());
            args.AddRange(keys);
            args.AddRange(argv);
            return await connection.ExecuteAsync("EVAL", args.ToArray());
        }

        internal static async Task<int?> CreateAndEvalAsync(this IRedisConnection connection, string scriptName, string[] keys,
            string[] argv, string fullScript = "")
        {
            string sha;
            if (!Scripts.ShaCollection.ContainsKey(scriptName))
            {
                
                if (Scripts.ScriptCollection.ContainsKey(scriptName))
                {
                    sha = await connection.ExecuteAsync("SCRIPT","LOAD", Scripts.ScriptCollection[scriptName]);
                }
                else if (!string.IsNullOrEmpty(fullScript))
                {
                    sha = await connection.ExecuteAsync("SCRIPT","LOAD", fullScript);
                }
                else
                {
                    throw new ArgumentException($"scriptName must be amongst predefined scriptNames or a full script provided, script: {scriptName} not found");
                }

                Scripts.ShaCollection[scriptName] = sha;
            }
            var args = new List<string>();
            args.Add(Scripts.ShaCollection[scriptName]);
            args.Add(keys.Count().ToString());
            args.AddRange(keys);
            args.AddRange(argv);
            return await connection.ExecuteAsync("EVALSHA", args.ToArray());

        }
        
        internal static int? CreateAndEval(this IRedisConnection connection, string scriptName, string[] keys,
            string[] argv, string fullScript = "")
        {
            string sha;
            if (!Scripts.ShaCollection.ContainsKey(scriptName))
            {
                
                if (Scripts.ScriptCollection.ContainsKey(scriptName))
                {
                    sha = connection.Execute("SCRIPT","LOAD", Scripts.ScriptCollection[scriptName]);
                }
                else if (!string.IsNullOrEmpty(fullScript))
                {
                    sha = connection.Execute("SCRIPT","LOAD", fullScript);
                }
                else
                {
                    throw new ArgumentException("scriptName must be amongst predefined scriptNames or a full script provided");
                }

                Scripts.ShaCollection[scriptName] = sha;
            }
            var args = new List<string>();
            args.Add(Scripts.ShaCollection[scriptName]);
            args.Add(keys.Count().ToString());
            args.AddRange(keys);
            args.AddRange(argv);
            return connection.Execute("EVALSHA", args.ToArray());
        }

        #endregion

        #region streamOperations
        public static StreamInfoBasic XInfoJustInfo(this IRedisConnection connection, string streamId)
        {
            throw new NotImplementedException();
        }

        public static StreamInfoFull XInfoFull(this IRedisConnection connection, string streamId, int count = -1)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<StreamGroupInfo> XInfoGroups(this IRedisConnection connection, string streamId)
        {
            throw new NotImplementedException();
        }
        public static async Task<IEnumerable<StreamGroupInfo>> XInfoGroupsAsync(this IRedisConnection connection, string streamId)
        {
            var res = await connection.ExecuteAsync("XINFO", "GROUPS", streamId);
            var groups = new List<StreamGroupInfo>();
            foreach(var groupHash in res.ToArray())
            {
                var objHash = groupHash?.ToObject<StreamGroupInfo>();
                if (objHash != null)
                {
                    groups.Add(objHash);
                }                
            }
            return groups;
        }

        public static IEnumerable<StreamConsumerInfo> XInfoGroup(this IRedisConnection connection, string streamId, string group)
        {
            throw new NotImplementedException();
        }

        public static Task<IEnumerable<StreamConsumerInfo>> XInfoGroupAsync(this IRedisConnection connection, string streamId, string group)
        {
            throw new NotImplementedException();
        }

        public static long XAck(this IRedisConnection connection, string streamId, string group, params string[] ids)
        {
            var args = new List<string> { streamId, group };
            args.AddRange(ids);
            return connection.Execute("XACK", args.ToArray());
        }

        public static async Task<string?> XAddAsync(this IRedisConnection connection, string streamId, object message, string messageId = "*", int maxLen = -1, string minId = "", bool trimApprox = true, bool makeStream = true)
        {
            var kvps = message.BuildHashSet();
            var args = new List<object> { streamId };
            if (!makeStream)
            {
                args.Add("NOMKSTREAM");
            }
            if (maxLen >= 0 && !string.IsNullOrEmpty(minId))
            {
                throw new ArgumentException("Only maxLen or minId may be set, not both");
            }
            else if (maxLen >= 0)
            {
                args.Add("MAXLEN");
                if (trimApprox)
                {
                    args.Add("~");
                }
                else
                {
                    args.Add("=");
                }
                args.Add(maxLen.ToString());
            }
            else if (!string.IsNullOrEmpty(minId))
            {
                args.Add("MINID");
                if (trimApprox)
                {
                    args.Add("~");
                }
                else
                {
                    args.Add("=");
                }
                args.Add(minId);
            }
            args.Add(messageId);
            foreach (var item in kvps)
            {
                args.Add(item.Key);
                args.Add(item.Value);
            }
            return await connection.ExecuteAsync("XADD", args.ToArray());
        }
        public static string? XAdd(this IRedisConnection connection, string streamId, object message, string messageId = "*", int maxLen = -1, string minId = "", bool trimApprox = true, bool makeStream = true)
        {
            var kvps = message.BuildHashSet();
            var args = new List<object> { streamId };
            if (!makeStream)
            {
                args.Add("NOMKSTREAM");
            }
            if (maxLen >= 0 && !string.IsNullOrEmpty(minId))
            {
                throw new ArgumentException("Only maxLen or minId may be set, not both");
            }
            else if (maxLen >= 0)
            {
                args.Add("MAXLEN");
                if(trimApprox)
                {
                    args.Add("~");
                }
                else
                {
                    args.Add("=");
                }
                args.Add(maxLen.ToString());
            }
            else if (!string.IsNullOrEmpty(minId))
            {
                args.Add("MINID");
                if (trimApprox)
                {
                    args.Add("~");
                }
                else
                {
                    args.Add("=");
                }
                args.Add(minId);
            }
            args.Add(messageId);
            foreach(var item in kvps)
            {
                args.Add(item.Key);
                args.Add(item.Value);
            }
            return connection.Execute("XADD", args.ToArray());
        }

        public static XAutoClaimResponse<T> XAutoClaim<T>(this IRedisConnection connection, string streamId, string group, string consumer, int minIdleTime, string startId, int count = -1)
            where T : notnull
        {
            var args = new List<string> { streamId, group, consumer, minIdleTime.ToString(), startId };
            if(count>-1)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = connection.Execute("XAUTOCLAIM", args.ToArray());            
            return new XAutoClaimResponse<T>(res.ToArray(), streamId);
        }

        public static async Task<XAutoClaimResponse<T>> XAutoClaimAsync<T>(this IRedisConnection connection, string streamId, string group, string consumer, int minIdleTime, string startId, int count = -1)
            where T : notnull
        {
            var args = new List<string> { streamId, group, consumer, minIdleTime.ToString(), startId };
            if (count > -1)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = await connection.ExecuteAsync("XAUTOCLAIM", args.ToArray());             
            return new XAutoClaimResponse<T>(res.ToArray(), streamId);
        }

        public static XAutoClaimResponse XAutoClaim(this IRedisConnection connection, string streamId, string group, string consumer, int minIdleTime, string startId, int count = -1)
        {
            var args = new List<string> { streamId, group, consumer, minIdleTime.ToString(), startId };
            if (count > -1)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = connection.Execute("XAUTOCLAIM", args.ToArray());
            return new XAutoClaimResponse(res.ToArray());
        }

        public static IEnumerable<string> XAutoClaimJustIds(this IRedisConnection connection, string streamId, string group, string consumer, int minIdleTime, string startId, int count = -1)
        {
            var args = new List<string> { streamId, group, consumer, minIdleTime.ToString(), startId };
            if (count > -1)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            args.Add("JUSTID");
            return (string[])connection.Execute("XAUTOCLAIM", args.ToArray());            
        }

        private static string[] ArrangeXClaimArgs(this IRedisConnection connection, string streamId, string group, string consumer, int minIdelTimeMs, string[] ids, int setIdleMs = -1, int setTimeMsUnix = -1, int setRetryCount = -1, bool force = false)
        {
            var args = new List<string> { streamId, group, consumer, minIdelTimeMs.ToString() };
            args.AddRange(ids);
            if (setIdleMs >= 0)
            {
                args.Add("IDLE");
                args.Add(setIdleMs.ToString());
            }
            if (setTimeMsUnix >= 0)
            {
                args.Add("TIME");
                args.Add(setTimeMsUnix.ToString());
            }
            if (setRetryCount >= 0)
            {
                args.Add("RETRYCOUNT");
                args.Add(setRetryCount.ToString());
            }
            if (force)
            {
                args.Add("FORCE");
            }
            return args.ToArray();                
        }

        public static XRangeResponse<T> XClaim<T>(this IRedisConnection connection, string streamId, string group, string consumer, int minIdelTimeMs, string[] ids, int setIdleMs = -1, int setTimeMsUnix = -1, int setRetryCount = -1, bool force = false)
            where T : notnull
        {
            var args = connection.ArrangeXClaimArgs(streamId, group, consumer, minIdelTimeMs, ids, setIdleMs, setTimeMsUnix, setRetryCount, force);
            var res = connection.Execute("XCLAIM", args);
            return new XRangeResponse<T>(res.ToArray(), streamId);
        }

        public static XRangeResponse XClaim(this IRedisConnection connection, string streamId, string group, string consumer, int minIdelTimeMs, string[] ids, int setIdleMs = -1, int setTimeMsUnix = -1, int setRetryCount = -1, bool force = false)
        {
            var args = connection.ArrangeXClaimArgs(streamId, group, consumer, minIdelTimeMs, ids, setIdleMs, setTimeMsUnix, setRetryCount, force);
            var res = connection.Execute("XCLAIM", args);
            return new XRangeResponse(res.ToArray());
        }

        public static IEnumerable<string> XClaimJustIds(this IRedisConnection connection, string streamId, string group, string consumer, int minIdelTimeMs, string[] ids, int setIdleMs = -1, int setTimeMsUnix = -1, int setRetryCount = -1, bool force = false)
        {
            var args = connection.ArrangeXClaimArgs(streamId, group, consumer, minIdelTimeMs, ids, setIdleMs, setTimeMsUnix, setRetryCount, force);
            args = args.Append("JUSTID").ToArray();
            var res = connection.Execute("XCLAIM", args);
            return (string[])res;
        }

        public static long XDel(this IRedisConnection connection, string streamId, params string[] ids)
        {
            var args = new List<string> { streamId };
            args.AddRange(ids);
            var res = connection.Execute("XDEL", args.ToArray());
            return res;
        }

        public static async Task<int?> XGroupCreateGroupAsync(this IRedisConnection connection, string streamId, string groupName, string startingId = "$", bool createStream = true)
        {
            var args = new List<string> { "CREATE", streamId, groupName, startingId };
            if (createStream)
            {
                args.Add("MKSTREAM");
            }
            return await connection.ExecuteAsync("XGROUP", args.ToArray());
        }

        public static int? XGroupCreateGroup(this IRedisConnection connection, string streamId, string groupName, string startingId = "$", bool createStream = true)
        {
            var args = new List<string> { "CREATE", streamId, groupName, startingId };
            if(createStream)
            {
                args.Add("MKSTREAM");
            }
            return connection.Execute("XGROUP", args.ToArray());
            
        }

        public static int? XGroupDeleteGroup(this IRedisConnection connection, string streamId, string groupName) => connection.Execute("XGROUP", "DESTROY", streamId, groupName);

        public static long XGroupCreateConsumer(this IRedisConnection connection, string streamId, string groupName, string consumerName) => connection.Execute("XGROUP", "CREATECONSUMER", streamId, groupName, consumerName);

        public static int? XGroupDeleteConsumer(this IRedisConnection connection, string streamId, string groupName, string consumerName) => connection.Execute("XGROUP", "DELCONSUMER", streamId, groupName, consumerName);

        public static bool? XGroupSetId(this IRedisConnection connection, string streamId, string groupName, string startingId) 
        {
            connection.Execute("XGROUP", "SETID", streamId, groupName, startingId);
            return true;
        } 

        public static long XLen(this IRedisConnection connection, string streamId) => connection.Execute("XLEN", streamId);

        public static IEnumerable<XPendingMessage> XPending(this IRedisConnection connection, string streamId, string groupName, int idleTime = -1, string start = "-", string end = "+", int count = 10, string consumer = "")
        {
            var args = new List<string> { streamId, groupName };
            if (idleTime > 0)
            {
                args.Add("IDLE");
                args.Add(idleTime.ToString());
            }
            args.Add(start);
            args.Add(end);
            args.Add("COUNT");
            args.Add(count.ToString());
            if(!string.IsNullOrEmpty(consumer))
            {
                args.Add(consumer); 
            }
            var res = connection.Execute("XPENDING", args.ToArray()).ToArray();
            var list = new List<XPendingMessage>();
            for(var i = 0; i < res.Length; i += 4)
            {
                var msg = new XPendingMessage(res.Skip(i).Take(3).ToArray());
                //var msg = new XPendingMessage(res[i..(i + 3)]);
                list.Add(msg);
            }
            return list;
        }

        public static XPendingReply XPending(this IRedisConnection connection, string streamId, string groupName)
        {
            var res = connection.Execute("XPENDING", streamId, groupName);            
            return new XPendingReply(res);
        }

        public static XRangeResponse<T> XRange<T>(this IRedisConnection connection, string streamId, string start, string end, int count = -1)
            where T : notnull
        {
            var args = new List<string> { streamId, start, end };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = connection.Execute("XRANGE", args.ToArray());
            return new XRangeResponse<T>(res.ToArray(), streamId);
        }

        public static XRangeResponse XRange(this IRedisConnection connection, string streamId, string start, string end, int count = -1)
        {
            var args = new List<string> { streamId, start, end };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = connection.Execute("XRANGE", args.ToArray());
            return new XRangeResponse(res.ToArray());
        }

        public static XRangeResponse<T> XRead<T>(this IRedisConnection connection, string streamId, string startingMessageId, int count = -1, int blockMilliseconds = -1)
            where T : notnull
        {
            var args = new List<string>();
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMilliseconds >= 0)
            {
                args.Add("BLOCK");
                args.Add(blockMilliseconds.ToString());
            }
            args.Add("STREAMS");
            args.Add(streamId);
            args.Add(startingMessageId);
            var res = connection.Execute("XREAD", args.ToArray());
            return new XRangeResponse<T>(res.ToArray(), streamId);            
        }

        public static async Task<XRangeResponse<T>> XReadAsync<T>(this IRedisConnection connection, string streamId, string startingMessageId, int count = -1, int blockMilliseconds = -1)
            where T : notnull
        {
            var args = new List<string>();
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMilliseconds >= 0)
            {
                args.Add("BLOCK");
                args.Add(blockMilliseconds.ToString());
            }
            args.Add("STREAMS");
            args.Add(streamId);
            args.Add(startingMessageId);
            var res = await connection.ExecuteAsync("XREAD", args.ToArray());
            return new XRangeResponse<T>(res.ToArray(), streamId);
        }

        public static XRangeResponse XRead(this IRedisConnection connection, string streamId, string startingMessageId, int count = -1, int blockMilliseconds = -1)
        {
            var args = new List<string>();
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMilliseconds >= 0)
            {
                args.Add("BLOCK");
                args.Add(blockMilliseconds.ToString());
            }
            args.Add("STREAMS");
            args.Add(streamId);
            args.Add(startingMessageId);
            var res = connection.Execute("XREAD", args.ToArray());
            return new XRangeResponse(res.ToArray());            
        }

        public static XRangeResponse XRead(this IRedisConnection connection, string[] streamIds, string[] startingMessageId, int count = -1, int blockMilliseconds = -1)
        {
            throw new NotImplementedException();
        }

        public static XRangeResponse<T> XReadGroup<T>(this IRedisConnection connection, string streamId, string startingId, string groupName, string consumerName, int count = -1, int blockMs = -1, bool noAck = false)
            where T : notnull
        {
            var args = new List<string> { streamId, startingId, groupName, consumerName };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMs > 0)
            {
                args.Add("BLOCK");
                args.Add(blockMs.ToString());
            }
            if (noAck)
            {
                args.Add("NOACK");
            }
            var res = connection.Execute("XREADGROUP");
            return new XRangeResponse<T>(res.ToArray(), streamId);
        }

        public static async Task<XRangeResponse<T>> XReadGroupAsync<T>(this IRedisConnection connection, string streamId, string startingId, string groupName, string consumerName, int count = -1, int blockMs = -1, bool noAck = false)
            where T : notnull
        {
            var args = new List<string> {"GROUP", groupName, consumerName };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMs > 0)
            {
                args.Add("BLOCK");
                args.Add(blockMs.ToString());
            }
            if (noAck)
            {
                args.Add("NOACK");
            }
            args.Add("STREAMS");
            args.Add(streamId);
            args.Add(startingId);
            var res = await connection.ExecuteAsync("XREADGROUP", args.ToArray());
            return new XRangeResponse<T>(res.ToArray(), streamId);
        }

        public static XRangeResponse XReadGroup(this IRedisConnection connection, string streamId, string startingId, string groupName, string consumerName, int count = -1, int blockMs = -1, bool noAck = false)
        {
            var args = new List<string> { streamId, startingId, groupName, consumerName };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMs > 0)
            {
                args.Add("BLOCK");
                args.Add(blockMs.ToString());
            }
            if (noAck)
            {
                args.Add("NOACK");
            }
            var res = connection.Execute("XREADGROUP");
            return new XRangeResponse(res.ToArray());
        }

        public static async Task<XRangeResponse> XReadGroupAsync(this IRedisConnection connection, string streamId, string startingId, string groupName, string consumerName, int count = -1, int blockMs = -1, bool noAck = false)
        {
            var args = new List<string> { streamId, startingId, groupName, consumerName };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            if (blockMs > 0)
            {
                args.Add("BLOCK");
                args.Add(blockMs.ToString());
            }
            if (noAck)
            {
                args.Add("NOACK");
            }
            var res = await connection.ExecuteAsync("XREADGROUP");
            return new XRangeResponse(res.ToArray());
        }

        public static XRangeResponse<T> XRevRange<T>(this IRedisConnection connection, string streamId, string endingId, string startingId, int count = -1)
            where T : notnull
        {
            var args = new List<string> { streamId, endingId, startingId };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = connection.Execute("XREVRANGE", args.ToArray());
            return new XRangeResponse<T>(res.ToArray(), streamId);
        }

        public static XRangeResponse XRevRange(this IRedisConnection connection, string streamId, string endingId, string startingId, int count = -1)
        {
            var args = new List<string> { streamId, endingId, startingId };
            if (count > 0)
            {
                args.Add("COUNT");
                args.Add(count.ToString());
            }
            var res = connection.Execute("XREVRANGE", args.ToArray());
            return new XRangeResponse(res);
        }

        public static long XTrim(this IRedisConnection connection, string streamId, string minId, bool approxomaiteTrim = true, int limit = -1)
        {
            var args = new List<string> { streamId, "MINID", approxomaiteTrim ? "~" : "=", minId };
            if (limit > 0)
            {
                args.Add("LIMIT");
                args.Add(limit.ToString());
            }
            return connection.Execute("XTRIM", args.ToArray());            
        }
        public static long XTrim(this IRedisConnection connection, string streamId, int maxLen, bool approxomaiteTrim = true)
        {
            var args = new List<string> { streamId, "MAXLEN", approxomaiteTrim ? "~" : "=", maxLen.ToString() };
            return connection.Execute("XTRIM", args.ToArray());            
        }

        #endregion

        #region JSONOperations
        public static string JsonGet(this IRedisConnection connection, string key, params string[] paths)
        {
            var args = new List<string> { key };
            args.AddRange(paths);
            return ((string)connection.Execute("JSON.GET", args.ToArray()));
        }

        public static async Task<T?> JsonGetAsync<T>(this IRedisConnection connection, string key, params string[] paths)
        {
            var args = new List<string> { key };
            args.AddRange(paths);
            var res = await connection.ExecuteAsync("JSON.GET", args.ToArray());
            return JsonSerializer.Deserialize<T>(((string)res));
        }

        public static async Task<string> JsonGetAsync(this IRedisConnection connection, string key, params string[] paths)
        {
            var args = new List<string> { key };
            args.AddRange(paths);
            return ((string)await connection.ExecuteAsync("JSON.GET", args.ToArray()));
        }
        #endregion
    }
}
