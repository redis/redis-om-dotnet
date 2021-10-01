using Newtonsoft.Json.Linq;

namespace NRedisPlus.RediSearch
{
    internal class JsonDiff : IObjectDiff
    {
        internal string Operation { get; }
        internal string Path { get; }
        internal JToken Value { get; }

        internal JsonDiff(string operation, string path, JToken value)
        {
            Operation = operation;
            Path = path;
            Value = value;
        }

        public string[] SerializeScriptArgs()
        {
            if (Value.Type == JTokenType.String)
                return new[] {Operation, Path, $"\"{Value.ToString()}\""};
            return new[] {Operation, Path, Value.ToString()};
        }

        public string Script => nameof(Scripts.JSON_DIFF_RESOLUTION);
    }
}