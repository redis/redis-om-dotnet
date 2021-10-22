using System;
using System.Linq;
using StackExchange.Redis;

namespace Redis.OM
{
    /// <summary>
    /// A generic reply from redis which can be explicitly used as an appropriate type.
    /// </summary>
    public class RedisReply : IConvertible
    {
#pragma warning disable SA1018
        private readonly RedisReply[] ? _values;
#pragma warning restore SA1018
        private readonly double? _internalDouble;
        private readonly int? _internalInt;
        private readonly string? _internalString;
        private readonly long? _internalLong;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReply"/> class.
        /// </summary>
        /// <param name="val">the value.</param>
        internal RedisReply(double val)
        {
            _internalDouble = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReply"/> class.
        /// </summary>
        /// <param name="val">the value.</param>
        internal RedisReply(string val)
        {
            _internalString = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReply"/> class.
        /// </summary>
        /// <param name="val">the value.</param>
        internal RedisReply(long val)
        {
            _internalLong = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReply"/> class.
        /// </summary>
        /// <param name="i">the value.</param>
        internal RedisReply(int i)
        {
            _internalInt = i;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReply"/> class.
        /// </summary>
        /// <param name="values">the values.</param>
        internal RedisReply(RedisReply[] values)
        {
            _values = values;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReply"/> class.
        /// </summary>
        /// <param name="result">the redisResult.</param>
        internal RedisReply(RedisResult result)
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
            }
        }

        /// <summary>
        /// implicitly converts the reply to a double.
        /// </summary>
        /// <param name="v">the <see cref="RedisReply"/>.</param>
        /// <returns>the double.</returns>
        /// <exception cref="InvalidCastException">thrown if reply could not be converted to a double.</exception>
        public static implicit operator double(RedisReply v)
        {
            if (v._internalDouble != null)
            {
                return (double)v._internalDouble;
            }

            if (v._internalString != null && double.TryParse(v._internalString, out var ret))
            {
                return ret;
            }

            if (v._internalInt != null)
            {
                return (double)v._internalInt;
            }

            if (v._internalLong != null)
            {
                return (double)v._internalLong;
            }

            throw new InvalidCastException("Could not cast to double");
        }

        /// <summary>
        /// implicitly converts the reply to a double.
        /// </summary>
        /// <param name="v">the <see cref="RedisReply"/>.</param>
        /// <returns>the double.</returns>
        /// <exception cref="InvalidCastException">thrown if reply could not be converted to a double.</exception>
        public static implicit operator double?(RedisReply v) => v._internalDouble;

        /// <summary>
        /// implicitly converts the reply to a double.
        /// </summary>
        /// <param name="d">the double.</param>
        /// <returns>the reply.</returns>
        public static implicit operator RedisReply(double d) => new (d);

        /// <summary>
        /// implicitly converts the reply to an array of replies.
        /// </summary>
        /// <param name="v">the original reply.</param>
        /// <returns>An array of replies.</returns>
        public static implicit operator RedisReply[](RedisReply v) => v._values ?? new[] { v };

        /// <summary>
        /// Implicitly converts an array of replies into a single reply.
        /// </summary>
        /// <param name="vals">The replies.</param>
        /// <returns>the single reply.</returns>
        public static implicit operator RedisReply(RedisReply[] vals) => new (vals);

        /// <summary>
        /// Converts a redis reply to a string implicitly.
        /// </summary>
        /// <param name="v">the reply.</param>
        /// <returns>the string.</returns>
        public static implicit operator string(RedisReply v) => v._internalString ?? string.Empty;

        /// <summary>
        /// implicitly converts a string into a redis reply.
        /// </summary>
        /// <param name="s">the string.</param>
        /// <returns>the reply.</returns>
        public static implicit operator RedisReply(string s) => new (s);

        /// <summary>
        /// implicitly converts the reply into an integer.
        /// </summary>
        /// <param name="v">the reply.</param>
        /// <returns>the integer.</returns>
        /// <exception cref="InvalidCastException">thrown if it could not be converted to an integer.</exception>
        public static implicit operator int(RedisReply v)
        {
            if (v._internalInt != null)
            {
                return (int)v._internalInt;
            }

            if (v._internalString != null && int.TryParse(v._internalString, out var ret))
            {
                return ret;
            }

            if (v._internalDouble != null)
            {
                return (int)v._internalDouble;
            }

            if (v._internalLong != null)
            {
                return (int)v._internalLong;
            }

            throw new InvalidCastException("Could not cast to int");
        }

        /// <summary>
        /// implicitly converts the reply to an integer.
        /// </summary>
        /// <param name="v">The redis reply.</param>
        /// <returns>the integer.</returns>
        public static implicit operator int?(RedisReply v) => v._internalInt;

        /// <summary>
        /// Converts an integer to a reply.
        /// </summary>
        /// <param name="i">the integer.</param>
        /// <returns>the reply.</returns>
        public static implicit operator RedisReply(int i) => new (i);

        /// <summary>
        /// converts a redis reply to a long.
        /// </summary>
        /// <param name="v">the reply.</param>
        /// <returns>the long.</returns>
        /// <exception cref="InvalidCastException">thrown if a long could not be parsed from the reply.</exception>
        public static implicit operator long(RedisReply v)
        {
            if (v._internalLong != null)
            {
                return (long)v._internalLong;
            }

            if (v._internalString != null && long.TryParse(v._internalString, out var ret))
            {
                return ret;
            }

            if (v._internalDouble != null)
            {
                return (long)v._internalDouble;
            }

            if (v._internalInt != null)
            {
                return (long)v._internalInt;
            }

            throw new InvalidCastException("Could not cast to long");
        }

        /// <summary>
        /// converts a redis reply to a long.
        /// </summary>
        /// <param name="v">the reply.</param>
        /// <returns>the long.</returns>
        public static implicit operator long?(RedisReply v) => v._internalLong;

        /// <summary>
        /// converts a long to a redis reply.
        /// </summary>
        /// <param name="l">the long.</param>
        /// <returns>the reply.</returns>
        public static implicit operator RedisReply(long l) => new (l);

        /// <summary>
        /// Converts the reply to an array of strings.
        /// </summary>
        /// <param name="v">the reply.</param>
        /// <returns>the array.</returns>
        public static implicit operator string[](RedisReply v) =>
            v.ToArray().Select(s => (string)s).ToArray();

        /// <summary>
        /// Converts the reply to an array of doubles.
        /// </summary>
        /// <param name="v">the reply.</param>
        /// <returns>The doubles.</returns>
        public static implicit operator double[](RedisReply v) =>
            v.ToArray().Select(d => (double)d).ToArray();

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_internalDouble != null)
            {
                return _internalDouble.ToString();
            }

            if (_internalLong != null)
            {
                return _internalLong.ToString();
            }

            if (_internalInt != null)
            {
                return _internalInt.ToString();
            }

            if (_internalString != null)
            {
                return _internalString;
            }

            return base.ToString();
        }

        /// <summary>
        /// Sends the collection to an array.
        /// </summary>
        /// <returns>the RedisReply as an array.</returns>
        public RedisReply[] ToArray() => _values ?? new[] { this };

        /// <inheritdoc/>
        public TypeCode GetTypeCode()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool ToBoolean(IFormatProvider provider)
        {
            return this == 1;
        }

        /// <inheritdoc/>
        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public double ToDouble(IFormatProvider provider)
        {
            return this;
        }

        /// <inheritdoc/>
        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public int ToInt32(IFormatProvider provider) => this;

        /// <inheritdoc/>
        public long ToInt64(IFormatProvider provider) => this;

        /// <inheritdoc/>
        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public float ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        /// <inheritdoc/>
        public object ToType(Type conversionType, IFormatProvider provider)
        {
            var underlyingType = Nullable.GetUnderlyingType(conversionType);
            if (underlyingType != null)
            {
                switch (underlyingType.Name)
                {
                    case "Int32":
                        return (int)this;
                    case "Int64":
                        return (long)this;
                    case "Single":
                        return (float)this;
                    case "Double":
                        return (double)this;
                }
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}
