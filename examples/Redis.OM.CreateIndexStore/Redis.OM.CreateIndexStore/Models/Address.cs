using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.OM.CreateIndexStore.Models
{
    public class Address
    {
        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }
        public string? City { get; set; }

        public Address(string addressLine1, string? city)
        {
            AddressLine1 = addressLine1;
            City = city;
        }
    }
}