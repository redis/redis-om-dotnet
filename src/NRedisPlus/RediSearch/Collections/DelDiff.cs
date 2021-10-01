using System;

namespace NRedisPlus.RediSearch
{
    public class DelDiff : IObjectDiff
    {
        public string[] SerializeScriptArgs()
        {
            return Array.Empty<string>();
        }

        public string Script =>nameof(Scripts.UNLINK);
    }
}