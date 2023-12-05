namespace Redis.OM;

internal static class RedisOMHttpUtil
{
    public static string ReadJsonSync(HttpResponseMessage msg)
    {
        return new StreamReader(msg.Content.ReadAsStream()).ReadToEnd();
    }
}