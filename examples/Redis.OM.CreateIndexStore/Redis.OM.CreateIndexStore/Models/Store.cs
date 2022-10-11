using Redis.OM.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.OM.CreateIndexStore.Models
{
    [Document(StorageType = StorageType.Hash)]
    public record Store
    {
        [RedisIdField][Indexed] public int Id { get; set; }

        [Indexed] public string FullAddress { get; set; } = null!;
        [Indexed] public string Name { get; set; } = null!;
    }
}