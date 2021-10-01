using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRedisPlus
{

    /// <summary>
    /// 
    /// </summary>
    public class RedisFastList : IList<string>
    {
        const string ADD_ITEM_SCRIPT = "local idx = redis.call('ZRANGE', KEYS[1], -1, -1, 'WITHSCORES')[2]\n" +
                                       "if(idx==nil) then\n"+
                                       "\t idx=0\n"+
                                       "else\n"+
                                       "\t idx = idx +1\n"+
                                       "end\n" +
                                       "return redis.call('ZADD', KEYS[1], idx, ARGV[1]..'-'..idx)";
        private IRedisConnection _connection;
        private string _keyName;
        private uint _chunkSize;        

        public RedisFastList(IRedisConnection connection, string keyName, uint chunkSize = 100)
        {
            _connection = connection;
            _keyName = keyName;
            _chunkSize = chunkSize;
        }
        public string this[int index] 
        { 
            get  
                {
                var res = _connection.ZRange(_keyName, index + 1, index + 1).FirstOrDefault();
                if(res == null) throw new IndexOutOfRangeException();
                return res.Substring(0, res.Length - res.LastIndexOf('-'));               
            }
            set  
            {
                _connection.ZRemRangeByRank(_keyName, index + 1, index + 1);
                var member = new SortedSetEntry
                {
                    Score = index + 1,
                    Member = $"{value}-{index + 1}"
                };
                _connection.ZAdd(_keyName, member);
            }
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public int Count => (int?)_connection.ZCard(_keyName) ?? 0;

        public bool IsSynchronized => false;

        public object SyncRoot => false;

        public void Add(string value)
        {
            _connection.Eval(ADD_ITEM_SCRIPT, new string[] { _keyName }, new string[] { value });
        }

        public void Clear()
        {
            _connection.Unlink(_keyName);
        }

        public bool Contains(string value)
        {
            foreach(var item in this)
            {
                if((string)item == value)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(string[] array, int index)
        {
            throw new NotImplementedException();
        }
        public IEnumerator GetEnumerator()
        {
            return new FastListEnumorator(_connection, _keyName, _chunkSize);
        }

        public int IndexOf(string value)
        {
            var index = 0;
            foreach(string item in this)
            {
                if (item == value)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public void Insert(int index, string value)
        {
            this[index] = value;
        }

        public bool Remove(string value)
        {
            var idx = IndexOf(value);
            if (idx >= 0)
            {
                RemoveAt(idx);
                return true;
            }
            return false;
                
        }

        public void RemoveAt(int index)
        {
            _connection.ZRemRangeByRank(_keyName, index + 1, index + 1);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return new FastListEnumorator(_connection, _keyName, _chunkSize);
        }
    }
}
