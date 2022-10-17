namespace Redis.OM.BasicMatchingQueries.Models;

using Redis.OM.Modeling;

[Document(StorageType = StorageType.Json, IndexName = "customer-idx")]
public class Customer
{
   [Searchable] public string FirstName { get; set; }
   [Searchable] public string LastName { get; set; }
   [Searchable] public string Email { get; set; }
   [Indexed(Sortable = true)] public int Age { get; set; }
   [Indexed] public bool IsActive { get; set; }
}
