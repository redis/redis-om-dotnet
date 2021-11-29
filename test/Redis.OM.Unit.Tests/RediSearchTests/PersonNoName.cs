using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Document]
    public class PersonNoName
    {
        [Indexed]
        public int Age { get; set; }
    }
}