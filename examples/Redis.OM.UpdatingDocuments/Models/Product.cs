namespace Redis.OM.UpdatingDocuments.Models;

using Redis.OM.Modeling;

[Document(IndexName = "products-idx", StorageType = StorageType.Json)]
public class Product
{
    [RedisIdField]
    [Indexed] 
    public Ulid Id { get; set; }

    [Indexed]
    public string Name { get; set; }
    public string Description { get; set; } 

    [Indexed(Sortable = true)]        
    public double Price { get; set; }

    [Indexed(Sortable = true)]  
    public DateTime DateAdded { get; set; }

    [Indexed(Sortable = true)]  
    public bool InStock { get; set; }
}