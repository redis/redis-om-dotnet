using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document]
    public class BasicTypeWithGeoLoc
    {
        public string Name { get; set; }
        public GeoLoc Location { get; set; }
    }
}