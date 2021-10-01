namespace NRedisPlus.RediSearch
{
    internal interface IObjectDiff
    {
        string[] SerializeScriptArgs();
        string Script { get; }
    }
}