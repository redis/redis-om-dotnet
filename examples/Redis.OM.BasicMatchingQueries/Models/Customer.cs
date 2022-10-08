namespace Redis.OM.BasicMatchingQueries.Models;

using Redis.OM.Modeling;

[Document(StorageType = StorageType.Json)]
public class Customer
{
   [Indexed] public string FirstName { get; set; }
   [Indexed] public string LastName { get; set; }
   [Indexed] public string Email { get; set; }
   [Indexed(Sortable = true)] public int Age { get; set; }
   [Indexed] public bool IsActive { get; set; }
   [Indexed] public Gender Gender { get; set; }
   [Indexed(CascadeDepth = 2)]
   public Address Address {get; set;}
}
