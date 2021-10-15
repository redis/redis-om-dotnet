using System.Collections.Generic;

namespace NRedisPlus
{
    internal class Scripts
    {
        internal const string JSON_DIFF_RESOLUTION = @"local key = KEYS[1]
local num_args = table.getn(ARGV)
for i=1, num_args, 3 do
    if 'ARRREM' == ARGV[i] then
        local index = redis.call('JSON.ARRINDEX', key, ARGV[i+1], ARGV[i+2])
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

        internal const string HASH_DIFF_RESOLUTION = @"
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

        internal const string UNLINK = @"
return redis.call('UNLINK',KEYS[1])";
            

        internal static readonly Dictionary<string, string> ScriptCollection = new Dictionary<string, string>
        {
            {nameof(JSON_DIFF_RESOLUTION), JSON_DIFF_RESOLUTION},
            {nameof(HASH_DIFF_RESOLUTION), HASH_DIFF_RESOLUTION},
            {nameof(UNLINK), UNLINK}
        };

        internal static Dictionary<string, string> ShaCollection { get; set; } = new Dictionary<string, string>();
    }
}