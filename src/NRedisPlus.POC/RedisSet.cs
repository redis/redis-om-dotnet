using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{
    public class RedisSet : ISet<string>
    {
        private IRedisConnection _connection;
        private string _keyName;
        private uint _chunkSize;
        public RedisSet(string keyName, IRedisConnection connection, uint chunkSize = 100)
        {
            _connection = connection;
            _keyName = keyName;
            _chunkSize = chunkSize;
        }
        public int Count => (int?)_connection.SCard(_keyName) ?? default(int);

        public bool IsReadOnly => false;

        public bool Add(string item)
        {
            return _connection.SAdd(_keyName, item) > 0;
        }

        public void Clear()
        {
            _connection.Unlink(_keyName);
        }

        public bool Contains(string item)
        {
            return _connection.SIsMember(_keyName, item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void ExceptWith(IEnumerable<string> other)
        {
            _connection.SRem(_keyName, other.ToArray());
        }

        public IEnumerator<string> GetEnumerator()
        {
            return new RedisSetEnumorator(_connection, _keyName, _chunkSize);
        }

        public void IntersectWith(IEnumerable<string> other)
        {
            var temp_key_name = _keyName + "interesection-set" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_key_name, other.ToArray());
            _connection.SInterStore(_keyName, _keyName, temp_key_name);
            _connection.Unlink(temp_key_name);            
        }

        public bool IsProperSubsetOf(IEnumerable<string> other)
        {
            var temp_set_key_name = _keyName + "candidate-super-set" + Guid.NewGuid().ToString();
            var temp_store_set_name = _keyName + "diff-store-set" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_store_set_name, temp_set_key_name, _keyName);
            var card = _connection.SCard(temp_store_set_name);            
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_store_set_name);
            return (other.Count() - Count) == card && card>=1;
        }

        public bool IsProperSupersetOf(IEnumerable<string> other)
        {
            var temp_set_key_name = _keyName + "candidate-sub-set" + Guid.NewGuid().ToString();
            var temp_store_set_name = _keyName + "diff-store-set" + Guid.NewGuid().ToString();            
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_store_set_name, _keyName, temp_set_key_name);
            var card = _connection.SCard(temp_store_set_name);
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_store_set_name);
            return (Count - other.Count()) == card && card >= 1;
        }

        public bool IsSubsetOf(IEnumerable<string> other)
        {
            var temp_set_key_name = _keyName + "candidate-super-set" + Guid.NewGuid().ToString();
            var temp_store_set_name = _keyName + "diff-store-set" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_store_set_name, temp_set_key_name, _keyName);
            var card = _connection.SCard(temp_store_set_name);
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_store_set_name);
            return (other.Count() - Count) == card;
        }

        public bool IsSupersetOf(IEnumerable<string> other)
        {
            var temp_set_key_name = _keyName + "candidate-sub-set" + Guid.NewGuid().ToString();
            var temp_store_set_name = _keyName + "diff-store-set" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_store_set_name, _keyName, temp_set_key_name);
            var card = _connection.SCard(temp_store_set_name);
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_store_set_name);
            return (Count - other.Count()) == card;
        }

        public bool Overlaps(IEnumerable<string> other)
        {
            var temp_set_key_name = _keyName + "candidate-sub-set" + Guid.NewGuid().ToString();
            var temp_store_set_name = _keyName + "diff-store-set" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_store_set_name, _keyName, temp_set_key_name);
            var card = _connection.SCard(temp_store_set_name);
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_store_set_name);
            return card < (Count+other.Count());
        }

        public bool Remove(string item)
        {
            return _connection.SRem(_keyName, item) == 1;

        }

        public bool SetEquals(IEnumerable<string> other)
        {
            if(Count != other.Count())
            {
                return false;
            }
            var temp_set_key_name = _keyName + "candidate-equal-set" + Guid.NewGuid().ToString();
            var temp_store_set_name = _keyName + "diff-store-set" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_store_set_name, _keyName, temp_set_key_name);
            var card = _connection.SCard(temp_store_set_name);
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_store_set_name);
            return card == 0;
        }

        public void SymmetricExceptWith(IEnumerable<string> other)
        {
            var temp_set_key_name = _keyName + "candidate-sym-set" + Guid.NewGuid().ToString();
            var temp_diff_left_key_name = _keyName + "diff-store-set-left" + Guid.NewGuid().ToString();
            var temp_diff_right_key_name = _keyName + "diff-store-set-right" + Guid.NewGuid().ToString();
            _connection.SAdd(temp_set_key_name, other.ToArray());
            _connection.SDiffStore(temp_diff_left_key_name, _keyName, temp_set_key_name);
            _connection.SDiffStore(temp_diff_right_key_name, temp_set_key_name, _keyName);
            _connection.SUnionStore(_keyName, temp_diff_left_key_name, temp_diff_right_key_name);
            _connection.Unlink(temp_set_key_name);
            _connection.Unlink(temp_diff_left_key_name);
            _connection.Unlink(temp_diff_right_key_name);
        }

        public void UnionWith(IEnumerable<string> other)
        {
            _connection.SAdd(_keyName, other.ToArray());
        }

        void ICollection<string>.Add(string item)
        {
            Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
