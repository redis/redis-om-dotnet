namespace Redis.OM;

public class RedisOMHttpUtil
{
    public static string ReadJsonSync(HttpResponseMessage msg)
    {
        return new StreamReader(msg.Content.ReadAsStream()).ReadToEnd();
    }
}