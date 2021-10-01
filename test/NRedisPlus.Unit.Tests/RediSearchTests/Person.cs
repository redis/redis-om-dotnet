using NRedisPlus.RediSearch;
using NRedisPlus.RediSearch.Attributes;
using System.Collections.Generic;

namespace NRedisPlus.Unit.Tests.RediSearchTests
{
    [Document(StorageType = StorageType.JSON, IndexName = "person-idx")]
    public partial class Person
    {
        [RedisIdField]
        public string Id { get; set; }

        public Person Mother { get; set; }

        [Searchable(Sortable = true)]        
        public string Name { get; set; }

        [GeoIndex(Aggregatable = true)]
        public GeoLoc? Home { get; set; }

        [GeoIndex(Aggregatable = true)]
        public GeoLoc? Work { get; set; }

        public Address Address { get; set; }

        public bool? IsEngineer { get; set; }

        [Indexed(Sortable = true)]
        public int? Age { get; set; }

        [Indexed(Sortable = true)]
        public double? Height { get; set; }

        [ListType]
        public List<string> NickNames { get; set; }

        [Indexed(Sortable = true)]        
        public string TagField { get; set; }

        [Indexed(Sortable = true)]
        public int? DepartmentNumber { get; set; }

        [Indexed(Sortable = true)]
        public double? Sales { get; set; }

        [Indexed(Sortable = true)]
        public double? SalesAdjustment { get; set; }

        [Indexed(Sortable = true)]
        public long? LastTimeOnline { get; set; }

        [Searchable(Aggregatable = true)]        
        public string TimeString { get; set; }
        
        [Indexed(Aggregatable = true)]
        public string Email { get; set; }
        
        [Indexed(Aggregatable =false)]
        public  string UnaggrateableField { get; set; }
    }
}
