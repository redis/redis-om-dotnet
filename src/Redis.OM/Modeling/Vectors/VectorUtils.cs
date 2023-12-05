using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Helper utilities for handling vectors in Redis.
    /// </summary>
    public static class VectorUtils
    {
        /// <summary>
        /// Converts array of doubles to a vector string.
        /// </summary>
        /// <param name="doubles">the doubles.</param>
        /// <returns>the vector string.</returns>
        public static string ToVecString(IEnumerable<double> doubles)
        {
            var bytes = doubles.SelectMany(BitConverter.GetBytes).ToArray();
            return BytesToVecStr(bytes);
        }

        /// <summary>
        /// Converts array of floats to a vector string.
        /// </summary>
        /// <param name="floats">the floats.</param>
        /// <returns>the vector string.</returns>
        public static string ToVecString(IEnumerable<float> floats)
        {
            var bytes = floats.SelectMany(BitConverter.GetBytes).ToArray();
            return BytesToVecStr(bytes);
        }

        /// <summary>
        /// Converts the double to a binary safe redis vector string.
        /// </summary>
        /// <param name="d">the double.</param>
        /// <returns>The binary safe redis vector string.</returns>
        public static string DoubleToVecStr(double d)
        {
            return BytesToVecStr(BitConverter.GetBytes(d));
        }

        /// <summary>
        /// Converts the bytes to a binary safe redis string.
        /// </summary>
        /// <param name="bytes">the bytes to convert.</param>
        /// <returns>the binary safe redis String.</returns>
        public static string BytesToVecStr(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                switch (b)
                {
                    case 0x08:
                        sb.Append("\\b");
                        break;
                    case 0x22:
                        sb.Append("\"");
                        break;
                    case >= 0x20 and <= 0x7f:
                        sb.Append((char)b);
                        break;
                    default:
                        sb.Append($"\\x{Convert.ToString(b, 16).PadLeft(2, '0')}");
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts Vector String to array of doubles.
        /// </summary>
        /// <param name="reply">the reply.</param>
        /// <returns>the doubles.</returns>
        /// <exception cref="ArgumentException">Thrown if unbalanced.</exception>
        public static double[] VecStrToDoubles(RedisReply reply)
        {
            var bytes = (byte[]?)reply ?? throw new InvalidCastException("Could not convert result to raw result.");
            if (bytes.Length % 8 != 0)
            {
                throw new ArgumentException("Unbalanced Vector String");
            }

            var doubles = new double[bytes.Length / 8];
            for (var i = 0; i < bytes.Length; i += 8)
            {
                doubles[i / 8] = BitConverter.ToDouble(bytes, i);
            }

            return doubles;
        }

        /// <summary>
        /// converts the vector bytes to an array of doubles.
        /// </summary>
        /// <param name="bytes">the bytes.</param>
        /// <returns>The doubles.</returns>
        /// <exception cref="ArgumentException">Thrown if unbalanced.</exception>
        public static double[] VectorBytesToDoubles(byte[] bytes)
        {
            if (bytes.Length % 8 != 0)
            {
                throw new ArgumentException("Unbalanced Vector String");
            }

            var doubles = new double[bytes.Length / 8];
            for (var i = 0; i < bytes.Length; i += 8)
            {
                doubles[i / 8] = BitConverter.ToDouble(bytes, i);
            }

            return doubles;
        }

        /// <summary>
        /// Converts Vector String to array of doubles.
        /// </summary>
        /// <param name="reply">the reply.</param>
        /// <returns>the doubles.</returns>
        /// <exception cref="ArgumentException">Thrown if unbalanced.</exception>
        public static double[] VecStrToDoubles(string reply)
        {
            var bytes = Encoding.ASCII.GetBytes(reply);
            return VectorBytesToDoubles(bytes);
        }

        /// <summary>
        /// Parses the bytes into an array of floats.
        /// </summary>
        /// <param name="bytes">the bytes.</param>
        /// <returns>the floats.</returns>
        /// <exception cref="ArgumentException">Thrown if bytes are unbalanced.</exception>
        public static float[] VectorBytesToFloats(byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
            {
                throw new ArgumentException("Unbalanced Vector String");
            }

            var floats = new float[bytes.Length / 4];
            for (var i = 0; i < bytes.Length; i += 4)
            {
                floats[i / 4] = BitConverter.ToSingle(bytes, i);
            }

            return floats;
        }

        /// <summary>
        /// Parses a vector string to an array of floats.
        /// </summary>
        /// <param name="reply">the reply.</param>
        /// <returns>The floats.</returns>
        /// <exception cref="ArgumentException">thrown if unbalanced.</exception>
        public static float[] VectorStrToFloats(RedisReply reply)
        {
            var bytes = (byte[]?)reply ?? throw new InvalidCastException("Could not convert result to raw result.");
            return VectorBytesToFloats(bytes);
        }

        /// <summary>
        /// Converts binary safe Redis blob to double.
        /// </summary>
        /// <param name="str">the string.</param>
        /// <returns>the double the string represents.</returns>
        public static double DoubleFromVecStr(string str)
        {
            var bytes = VecStrToBytes(str);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Converts the binary safe vector string from Redis to an array of bytes.
        /// </summary>
        /// <param name="str">the string to convert back to bytes.</param>
        /// <returns>the bytes from the string.</returns>
        public static byte[] VecStrToBytes(string str)
        {
            var bytes = new List<byte>();
            var i = 0;
            while (i < str.Length)
            {
                if (str[i] == '\\' && i + 1 < str.Length && str[i + 1] == '\\')
                {
                    bytes.Add((byte)'\\');
                    i += 2;
                }
                else if (str[i] == '\\' && i + 3 < str.Length && str[i + 1] == 'x')
                {
                    // byte literal, interpret from hex.
                    bytes.Add(byte.Parse(str.Substring(i + 2, 2), NumberStyles.HexNumber));
                    i += 4;
                }
                else
                {
                    bytes.Add((byte)str[i]);
                    i++;
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Converts doubles to array of bytes.
        /// </summary>
        /// <param name="doubles">the doubles.</param>
        /// <returns>the array of bytes.</returns>
        internal static byte[] GetBytes(this double[] doubles) => doubles.SelectMany(BitConverter.GetBytes).ToArray();

        /// <summary>
        /// Converts floats to array of bytes.
        /// </summary>
        /// <param name="floats">the floats.</param>
        /// <returns>the array of bytes.</returns>
        internal static byte[] GetBytes(this float[] floats) => floats.SelectMany(BitConverter.GetBytes).ToArray();
    }
}