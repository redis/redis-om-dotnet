using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.AutoSuggestionTest
{
    [Document(StorageType = StorageType.Json, IndexName = "airport-idx")]
    [AutoSuggestion(Payload = true,Key ="sugg:airport:name")]
    public partial class Airport
    {
        [RedisIdField]
        public string Id { get; set; }
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string Code { get; set; }
        [Indexed]
        public string State { get; set; }
    }
}
