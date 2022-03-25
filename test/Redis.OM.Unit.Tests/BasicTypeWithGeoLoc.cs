using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    public class BasicTypeWithGeoLoc
    {
        public string Name { get; set; }
        public GeoLoc Location { get; set; }
    }
}