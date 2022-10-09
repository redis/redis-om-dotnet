using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.OM.CreateIndexStore
{
    public class Address
    {
        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }
        public string? City { get; set; }

        public Address(string addressLine1, string? city)
        {
            this.AddressLine1 = addressLine1;
            this.City = city;
        }
    }
}