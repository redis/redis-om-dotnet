using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{
    public class RedisHashScanner : ICollection<string>
    {
        private RedisHash _hash;
        private IRedisConnection _connection;
        private string _keyName;
        private bool _valueScanner;
        public RedisHashScanner(string keyName, RedisHash hash, IRedisConnection connection, bool valueScanner = false)
        {
            _hash = hash;
            _connection = connection;
            _keyName = keyName;
            _valueScanner = valueScanner;
        }
        public int Count => _hash.Count;

        public bool IsReadOnly => true;

        public void Add(string item)
        {
            throw new InvalidOperationException("Illegal operation attempted - Collection is readonly");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Illegal operation attempted - Collection is readonly");
        }

        public bool Contains(string item)
        {
            if (!_valueScanner)
            {
                return _hash[item] != null;
            }
            else
            {
                foreach(var val in this)
                {
                    if (val == item)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<string> GetEnumerator()
        {
            if (_valueScanner)
            {
                return new RedisHashValueEnumorator(_connection, _keyName);
            }
            else
            {
                return new RedisHashKeyEnumorator(_connection, _keyName);
            }
        }

        public bool Remove(string item)
        {
            return _hash.Remove(item);            
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
