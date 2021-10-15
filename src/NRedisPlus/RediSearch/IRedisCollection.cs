using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus.RediSearch
{
    public interface IRedisCollection<T> : IOrderedQueryable<T>
    {
        void Save();
        ValueTask SaveAsync();
        string Insert(T item);
        Task<string> InsertAsync(T item);
        Task<T?> FindByIdAsync(string id);
        T? FindById(string id);
    }
}
