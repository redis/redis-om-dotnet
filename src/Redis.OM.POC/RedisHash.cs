using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;

namespace Redis.OM
{
    public class RedisHash : IDictionary<string, string>
    {
        private IRedisConnection _connection;
        private readonly string _keyName;

        public RedisHash(IRedisConnection connection, string keyName)
        {
            _connection = connection;
            _keyName = keyName;
        }

        public string this[string key] 
        { 
            get => _connection.HMGet(_keyName, key).FirstOrDefault() ?? ""; 
            set => _connection.HSet(_keyName, new KeyValuePair<string, object>(key,value)); 
        }

        public ICollection<string> Keys => new RedisHashScanner(_keyName, this, _connection, false);

        public ICollection<string> Values => new RedisHashScanner(_keyName, this, _connection, true);

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(string key, string value)
        {
            this[key] = value;            
        }

        public void Add(KeyValuePair<string, string> item)
        {
            this[item.Key] = item.Value;
        }

        public void Clear()
        {
            _connection.Unlink(_keyName);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return this[item.Value] == item.Key;
        }

        public bool ContainsKey(string key)
        {
            return this[key] != null;
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return new RedisHashEnumorator(_connection, _keyName);
        }

        public bool Remove(string key)
        {
            return _connection.HDel(_keyName, key);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out string value)
        {
            value = this[key];
            return value !=null;            
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new RedisHashEnumorator(_connection, _keyName);
        }
    }
}
