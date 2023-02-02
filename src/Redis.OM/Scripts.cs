using System.Collections.Generic;

namespace Redis.OM
{
    /// <summary>
    /// Holds the scripts.
    /// </summary>
    internal class Scripts
    {
        /// <summary>
        /// resolves a difference in JSON.
        /// </summary>
        internal const string JsonDiffResolution = @"local key = KEYS[1]
local num_args = table.getn(ARGV)
for i=1, num_args, 3 do
    if 'ARRREM' == ARGV[i] then
        local index = redis.call('JSON.ARRINDEX', key, ARGV[i+1], ARGV[i+2])[1]
        if index>=0 then
            redis.call('JSON.ARRPOP', key, ARGV[i+1], index)
        end
    else
        if 'DEL' == ARGV[i] then
            redis.call('JSON.DEL',key,ARGV[i+1])
        else
            redis.call(string.format('JSON.%s', ARGV[i]), key, ARGV[i+1], ARGV[i+2])
        end
    end
end
";

        /// <summary>
        /// resolves a difference in a hash.
        /// </summary>
        internal const string HashDiffResolution = @"
local key = KEYS[1]
local num_args = table.getn(ARGV)
local num_fields_to_set = ARGV[1]
local end_index = num_fields_to_set*2+1
local args = {}
for i=2, end_index, 2 do
    args[i-1] = ARGV[i]
    args[i] = ARGV[i+1]
end
if table.getn(args)>0 then
    redis.call('HSET',key,unpack(args))
end
if end_index < num_args then
    local second_op
    args = {}
    for i = end_index+1, num_args, 1 do
        args[i-end_index] = ARGV[i]
    end
    redis.call('HDEL',key,unpack(args))
end
";

        /// <summary>
        /// unlinks a key.
        /// </summary>
        internal const string Unlink = @"
return redis.call('UNLINK',KEYS[1])";

        /// <summary>
        /// Unlinks and sets a key for a Hash model.
        /// </summary>
        internal const string UnlinkAndSetHash = @"
redis.call('UNLINK',KEYS[1])
local num_fields = ARGV[1]
local end_index = num_fields * 2 + 1
local args = {}
for i = 2, end_index, 2 do
    args[i-1] = ARGV[i]
    args[i] = ARGV[i+1]
end
redis.call('HSET',KEYS[1],unpack(args))
return 0
";

        /// <summary>
        /// Unlinks a JSON object and sets the key again with a fresh new JSON object.
        /// </summary>
        internal const string UnlinkAndSendJson = @"
redis.call('UNLINK', KEYS[1])
redis.call('JSON.SET', KEYS[1], '.', ARGV[1])
return 0
";

        /// <summary>
        /// Conditionally calls a hset if a key doesn't exist.
        /// </summary>
        internal const string HsetIfNotExists = @"
local exists = redis.call('EXISTS', KEYS[1])
if exists ~= 1 then
    local hashArgs = {}
    local expiry = tonumber(ARGV[1])
    for i = 2, table.getn(ARGV) do
        hashArgs[i-1] = ARGV[i]
    end
    redis.call('HSET', KEYS[1], unpack(hashArgs))
    if expiry > 0 then
        redis.call('PEXPIRE', KEYS[1], expiry)
    end 
    return 1    
end
return 0
";

        /// <summary>
        /// replaces hash if key exists.
        /// </summary>
        internal const string ReplaceHashIfExists = @"
local exists = redis.call('EXISTS', KEYS[1])
if exists == 1 then
    local hashArgs = {}
    local expiry = tonumber(ARGV[1])
    for i = 2, table.getn(ARGV) do
        hashArgs[i-1] = ARGV[i]
    end
    redis.call('UNLINK', KEYS[1])
    redis.call('HSET', KEYS[1], unpack(hashArgs))
    if expiry > 0 then
        redis.call('PEXPIRE', KEYS[1], expiry)
    end
    return 1
end
return 0
";

        /// <summary>
        /// Sets a Json object, if the object is set, and there is an expiration, also set expiration.
        /// </summary>
        internal const string JsonSetWithExpire = @"
local expiry = tonumber(ARGV[1])
local jsonArgs = {}
for i = 2, table.getn(ARGV) do
    jsonArgs[i-1] = ARGV[i]
end
local wasAdded = redis.call('JSON.SET', KEYS[1], unpack(jsonArgs))
if wasAdded ~= false then
    if expiry > 0 then
        redis.call('PEXPIRE', KEYS[1], expiry)
    else
        redis.call('PERSIST', KEYS[1])
    end
    return 1
end
return 0
";

        /// <summary>
        /// The scripts.
        /// </summary>
        internal static readonly Dictionary<string, string> ScriptCollection = new ()
        {
            { nameof(JsonDiffResolution), JsonDiffResolution },
            { nameof(HashDiffResolution), HashDiffResolution },
            { nameof(Unlink), Unlink },
            { nameof(UnlinkAndSetHash), UnlinkAndSetHash },
            { nameof(UnlinkAndSendJson), UnlinkAndSendJson },
            { nameof(HsetIfNotExists), HsetIfNotExists },
            { nameof(ReplaceHashIfExists), ReplaceHashIfExists },
            { nameof(JsonSetWithExpire), JsonSetWithExpire },
        };

        /// <summary>
        /// Gets or sets collection of SHAs.
        /// </summary>
        internal static Dictionary<string, string> ShaCollection { get; set; } = new ();
    }
}
