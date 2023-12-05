using System.Collections.Generic;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    [Document(StorageType = StorageType.Json, IndexName = "person-idx")]
    public partial class Person
    {
        [RedisIdField]
        [Indexed]
        public string Id { get; set; }

        public Person Mother { get; set; }

        [Searchable]        
        public string Name { get; set; }

        [Indexed(Aggregatable = true)]
        public GeoLoc? Home { get; set; }

        [Indexed(Aggregatable = true)]
        public GeoLoc? Work { get; set; }

        [Indexed(CascadeDepth = 2)]
        public Address Address { get; set; }

        public bool? IsEngineer { get; set; }

        [Indexed(Sortable = true)]
        public int? Age { get; set; }

        [Indexed(Sortable = true)]
        public double? Height { get; set; }

        [Indexed(Sortable = true)]
        public decimal? Salary { get; set; }

        [ListType]
        [Indexed]
        public string[] NickNames { get; set; }

        [Indexed]
        public List<string> NickNamesList { get; set; }

        [Indexed]        
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
        
        [Indexed]
        public string Email { get; set; }
        
        [Indexed(Aggregatable =false)]
        public  string UnaggrateableField { get; set; }

        [Indexed]
        public string? NullableStringField { get; set; }

        [Indexed(Aggregatable = true)] public string FirstName { get; set; }
        [Indexed(Aggregatable = true)] public string LastName { get; set; }


    }
}
