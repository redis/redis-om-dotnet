using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Model;
using Redis.OM.Schema;

namespace Redis.OM.Unit.Tests
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
