using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests
{
    [Document(IndexName = "employees_idx", StorageType = StorageType.Hash)]
    public class HashObjectWithTwoPropertiesWithMatchingPrefixes
    {
        [Searchable(Sortable = true)] public string Name { get; set; }
    
        [Searchable(Aggregatable = true)] public string Location { get; set; }
    
        [Searchable(Aggregatable = true)] public int? LocationNumber { get; set; }
    }
}