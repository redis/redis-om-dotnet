using System.Collections.Generic;
using NRedisPlus.Model;
using NRedisPlus.RediSearch.Attributes;
using NRedisPlus.RediSearch;
using NRedisPlus.Schema;

namespace NRedisPlus.Unit.Tests.RediSearchTests
{
    [Document(StorageType = StorageType.Hash, IndexName = "hash-person-idx")]
    public class HashPerson
    {
        [RedisIdField]
        public string Id { get; set; }

        public HashPerson Mother { get; set; }

        [Searchable(Sortable = true)]        
        public string Name { get; set; }

        [Indexed]
        public GeoLoc? Home { get; set; }

        [Indexed]
        public GeoLoc? Work { get; set; }

        public Address Address { get; set; }

        public bool? IsEngineer { get; set; }

        [Indexed(Sortable = true)]
        public int? Age { get; set; }

        [Indexed(Sortable = true)]
        public double? Height { get; set; }

        [ListType]
        public List<string> NickNames { get; set; }

        [Indexed]        
        public string TagField { get; set; }

        [Indexed(Sortable = true)]
        public int? DepartmentNumber { get; set; }

        [Indexed(Sortable = true)]
        public double? Sales { get; set; }

        [Indexed(Sortable = true)]
        public double? SalesAdjustment { get; set; }

        [Indexed]
        public long? LastTimeOnline { get; set; }

        [Searchable]        
        public string TimeString { get; set; }
        [Indexed]
        public string Email { get; set; }
    }
}