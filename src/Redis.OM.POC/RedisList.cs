using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redis.OM.Contracts;

namespace Redis.OM
{
    public class RedisList : IList<string>
    {        
        private IRedisConnection _connection;
        private string _keyName;
        private uint _chunkSize;
        private const string DELETION_TEXT = "DELETE ME";

        public RedisList(IRedisConnection connection, string keyName, uint chunkSize = 100)
        {
            _connection = connection;
            _keyName = keyName;
            _chunkSize = chunkSize;
        }

        public string this[int index] 
        { 
            get => _connection.LIndex(_keyName, index); 
            set => _connection.LSet(_keyName,index, value); 
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public int Count => (int?)_connection.LLen(_keyName) ?? default(int);

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public int Add(string value)
        {
            _connection.RPush(_keyName, value);
            return Count;
        }

        public void Clear()
        {
            _connection.Unlink(_keyName);
        }

        public bool Contains(string value)
        {
            var pos = _connection.LPos(_keyName, value);
            return pos >= 0;
        }

        public void CopyTo(Array array, int index)
        {
            array = _connection.LRange(_keyName, index, -1).ToArray();            
        }

        public IEnumerator GetEnumerator()
        {
            return new RedisListEnumorator(_connection, _keyName, _chunkSize);
        }

        public int IndexOf(string value)
        {
            return (int)_connection.LPos(_keyName, value);
        }

        public void Insert(int index, string value)
        {
            _connection.LSet(_keyName, index, value);
        }

        public void Remove(string value)
        {
            _connection.LRem(_keyName, value);

        }

        public void RemoveAt(int index)
        {
            Insert(index, DELETION_TEXT);
            Remove(DELETION_TEXT);
        }

        void ICollection<string>.Add(string item)
        {
            Add(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        bool ICollection<string>.Remove(string item)
        {
            Remove(item);
            return true;
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<string> values)
        {
            _connection.RPush(_keyName, values.ToArray());
        }
    }

    public class RedisListEnumorator : IEnumerator<string>
    {
        private string[] _chunk;
        private int _chunkIndex = -1;
        private int _offset = 0;
        private readonly uint _chunkSize;
        private IRedisConnection _connection;
        private readonly string _keyName;

        string IEnumerator<string>.Current => Current();

        object IEnumerator.Current => Current();

        public void Dispose()
        {
            //nothign to free up
        }

        public RedisListEnumorator(IRedisConnection connection, string keyName, uint chunkSize)
        {
            _connection = connection;
            _keyName = keyName;
            _chunkSize = chunkSize;
            _chunk = new string[0];
        }

        private void GetNextChunk()
        {            
            _chunk = _connection.LRange(_keyName, _offset, _offset + _chunkSize - 1); // advance to the next full chunk            
            _offset += _chunk.Length;
            _chunkIndex = 0;
        }

        private string Current() 
        {
            if (_chunkIndex == -1)
            {
                GetNextChunk();
            }
            return _chunk[_chunkIndex];
        }

        public bool MoveNext()
        {
            if (_chunkIndex == -1  || _chunkIndex + 1 >= _chunk.Length)
            {
                GetNextChunk();
                return _chunkIndex < _chunk.Length;
            }
            _chunkIndex++;
            return _chunkIndex < _chunk.Length;
        }

        public void Reset()
        {
            _offset = 0;
            _chunkIndex = -1;
        }
    }
}
