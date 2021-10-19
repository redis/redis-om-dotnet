using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRedisPlus.Model;
using NRedisPlus.Schema;

namespace NRedisPlus.Unit.Tests
{
    [Document(IndexName = "address-idx", StorageType = StorageType.Hash)]
    public partial class Address
    {
        public string StreetAddress { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }
}
