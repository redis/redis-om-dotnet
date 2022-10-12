using System.Collections.Generic;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Document(StorageType = StorageType.Json)]
    public class ObjectWithEmbeddedArrayOfObjects
    {
        [Indexed(JsonPath = "$.City")]
        [Indexed(JsonPath = "$.State")]
        [Indexed(JsonPath = "$.AddressType")]
        [Indexed(JsonPath = "$.Boolean")]
        [Indexed(JsonPath = "$.Guid")]
        [Indexed(JsonPath = "$.Ulid")]
        public Address[] Addresses { get; set; }
        
        [Indexed(JsonPath = "$.City")]
        [Indexed(JsonPath = "$.State")]
        public List<Address> AddressList { get; set; }

        [Indexed]
        public string Name { get; set; }
        
        [Indexed] public int Numeric { get; set; }
    }
}