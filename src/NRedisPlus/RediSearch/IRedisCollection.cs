using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NRedisPlus.RediSearch
{
    public interface IRedisCollection<T> : IQueryable<T>, IOrderedQueryable<T>
    {
        void Save();
        void Insert(T item);
        void AddRange(IEnumerable<T> items);
    }
}
