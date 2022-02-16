using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Document(IndexName = "empty-index")]
    public class ClassForEmptyRedisCollection
    {
        [Indexed] public string TagField { get; set; }
    }
}