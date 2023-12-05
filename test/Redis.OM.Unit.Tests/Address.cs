using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(IndexName = "address-idx", StorageType = StorageType.Json)]
    public partial class Address
    {
        public string StreetName { get; set; }
        public string ZipCode { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Indexed]
        public AddressType? AddressType { get; set; }
        [Indexed] public string City { get; set; }
        [Indexed] public string State { get; set; }
        [Indexed(CascadeDepth = 1)] public Address ForwardingAddress { get; set; }
        [Indexed] public GeoLoc? Location { get; set; }
        [Indexed] public int? HouseNumber { get; set; }
        [Indexed] public bool? Boolean { get; set; }
        [Indexed] public Ulid? Ulid { get; set; }
        [Indexed] public Guid? Guid { get; set; }
    }

    public enum AddressType
    {
        Home,
        Work,
        Vacation
    }
}
