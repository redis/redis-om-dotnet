namespace Redis.OM.BasicMatchingQueries.Models;

using Redis.OM.Modeling;

[Document(IndexName = "address-idx", StorageType = StorageType.Json)]
public partial class Address
{
    public string StreetName { get; set; }
    public string ZipCode { get; set; }
    [Indexed] public string City { get; set; }
    [Indexed] public string State { get; set; }
    [Indexed(CascadeDepth = 1)] public Address ForwardingAddress { get; set; }
    [Indexed] public GeoLoc Location { get; set; }
    [Indexed] public int HouseNumber { get; set; }
}