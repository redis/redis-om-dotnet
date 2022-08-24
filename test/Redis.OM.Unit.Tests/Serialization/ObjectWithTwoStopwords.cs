using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(Stopwords = new string[] {"foo", "bar"})]
    public class ObjectWithTwoStopwords
    {
        [Indexed]
        public string Name { get; set; }
    }
}