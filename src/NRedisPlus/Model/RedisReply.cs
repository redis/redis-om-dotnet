using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NRedisPlus
{
    public class RedisReply : IConvertible
    {
        protected RedisReply[]? _values;        
        protected readonly double? _internalDouble;
        protected readonly int? _internalInt;
        protected readonly string? _internalString;
        protected readonly long? _internalLong;

        public RedisReply(double val)
        {
            _internalDouble = val;
        }

        public RedisReply(string val)
        {
            _internalString = val;
        }

        public RedisReply(long val)
        {
            _internalLong = val;
        }

        public RedisReply(int i)
        {
            _internalInt = i;
        }

        public RedisReply(RedisReply[] values)
        {
            _values = values;
        }

        public RedisReply(RedisResult result)
        {
            switch (result.Type)
            {
                case ResultType.None:
                    break;
                case ResultType.SimpleString:
                case ResultType.BulkString:
                    _internalString = (string)result;
                    break;
                case ResultType.Error:
                    break;                
                case ResultType.Integer:
                    _internalLong = (long)result;
                    break;
                case ResultType.MultiBulk:
                    _values = ((RedisResult[])result).Select(x => new RedisReply(x)).ToArray();
                    break;
                default:
                    break;
            }
        }

        public static implicit operator double(RedisReply v)
        {            
            if (v._internalDouble != null)
                return (double)v._internalDouble;
            double ret;
            if (v._internalString != null && double.TryParse(v._internalString, out ret))
            {
                return ret;                    
            }
            if (v._internalInt != null)
                return (double)v._internalInt;
            if (v._internalLong != null)
                return (double)v._internalLong;
            throw new InvalidCastException("Could not cast to double");
        }
        public static implicit operator double?(RedisReply v)=>v?._internalDouble;
        public static implicit operator RedisReply(double d)=>new RedisReply(d);        
        
        public static implicit operator RedisReply[](RedisReply v)=>v._values ?? new RedisReply[] { v };
        public static implicit operator RedisReply(RedisReply[] vals)=>new RedisReply(vals);

        public static implicit operator string(RedisReply v)=>v._internalString ?? string.Empty;
        public static implicit operator RedisReply(string s)=>new RedisReply(s);
        
        public static implicit operator int(RedisReply v) 
        {
            if (v._internalInt != null)
                return (int)v._internalInt;
            int ret;
            if (v._internalString != null && int.TryParse(v._internalString, out ret))
            {
                return ret;
            }
            if (v._internalDouble != null)
                return (int)v._internalDouble;
            if (v._internalLong != null)
                return (int)v._internalLong;
            throw new InvalidCastException("Could not cast to int");
        }
        public static implicit operator int?(RedisReply v)=>v._internalInt;
        public static implicit operator RedisReply(int i)=>new RedisReply(i);

        public static implicit operator long(RedisReply v)
        {
            if (v._internalLong != null)
                return (long)v._internalLong;
            long ret;
            if (v._internalString != null && long.TryParse(v._internalString, out ret))
            {
                return ret;
            }
            if (v._internalDouble != null)
                return (long)v._internalDouble;
            if (v._internalInt != null)
                return (long)v._internalInt;
            throw new InvalidCastException("Could not cast to long");
        }
        public static implicit operator long?(RedisReply v)=>v?._internalLong;
        public static implicit operator RedisReply(long l)=>new RedisReply(l);        

        public static implicit operator string[](RedisReply v)=>v?.ToArray().Select(s=>(string)s).ToArray() ?? new string[0];
        public static implicit operator double[](RedisReply v)=>v?.ToArray().Select(d=>(double)d).ToArray() ?? new double[0];

        public static implicit operator SortedSetEntry(RedisReply v)
        {
            var arr = v.ToArray();
            return new SortedSetEntry
            {
                Member = arr[0],
                Score = (double)arr[1]
            };
        }
        public static implicit operator SortedSetEntry[](RedisReply v)
        {
            var arr = v.ToArray();
            var response = new List<SortedSetEntry>();
            for (var i = 0; i < arr.Length; i += 2)
            {
                response.Add(new SortedSetEntry
                {
                    Member = arr[i],
                    Score = double.Parse(arr[i+1])
                });
            }
            return response.ToArray();
        }

        public override string ToString()
        {
            if (_internalDouble != null)
                return _internalDouble.ToString();
            if (_internalLong != null)
                return _internalLong.ToString();
            if (_internalInt != null)
                return _internalInt.ToString();
            if (_internalString != null)
                return _internalString;
            return base.ToString();
        }

        public RedisReply[] ToArray() => _values ?? new RedisReply[1] { this };

        public TypeCode GetTypeCode()
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return this == 1;
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider provider)
        {
            return this;
        }

        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider provider) => this;

        public long ToInt64(IFormatProvider provider) => this;

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            var underlyingType = Nullable.GetUnderlyingType(conversionType);
            if (underlyingType != null)
            {
                switch (underlyingType.Name)
                {
                    case "Int32":
                        return (int) this;
                    case "Int64":
                        return (long) this;
                    case "Single":
                        return (float) this;
                    case "Double":
                        return (double) this;
                }
            }

            
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}
