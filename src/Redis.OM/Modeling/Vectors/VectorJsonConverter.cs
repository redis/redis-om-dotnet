using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Converts the provided object to a json vector.
    /// </summary>
    internal class VectorJsonConverter : JsonConverter<object>
    {
        private readonly VectorizerAttribute _vectorizerAttribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorJsonConverter"/> class.
        /// </summary>
        /// <param name="attribute">the attribute that will be used for vectorization.</param>
        internal VectorJsonConverter(VectorizerAttribute attribute)
        {
            _vectorizerAttribute = attribute;
        }

        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            reader.Read();
            var res = JsonSerializer.Deserialize(reader.GetString() !, typeToConvert);
            reader.Read();
            reader.Read(); // Vector
            reader.Read(); // start array
            for (var i = 0; i < _vectorizerAttribute.Dim; i++)
            {
                reader.Read(); // each item
            }

            reader.Read(); // end array
            return res;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            writer.WriteStringValue(JsonSerializer.Serialize(value));
            var bytes = _vectorizerAttribute.Vectorize(value);
            var jagged = SplitIntoJaggedArray(bytes, _vectorizerAttribute.VectorType == VectorType.FLOAT32 ? 4 : 8);
            writer.WritePropertyName("Vector");
            if (_vectorizerAttribute.VectorType == VectorType.FLOAT32)
            {
                var floats = jagged.Select(a => BitConverter.ToSingle(a, 0)).ToArray();
                writer.WriteStartArray();
                foreach (var f in floats)
                {
                    writer.WriteNumberValue(f);
                }

                writer.WriteEndArray();
            }
            else
            {
                var doubles = jagged.Select(BitConverter.ToDouble).ToArray();
                writer.WriteStartArray();
                foreach (var d in doubles)
                {
                    writer.WriteNumberValue(d);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => true;

        /// <summary>
        /// Converts input bytes to Jagged array.
        /// </summary>
        /// <param name="bytes">the bytes to parse.</param>
        /// <param name="numBytesPerArray">Size of the jagged arrays.</param>
        /// <returns>A jagged array of bytes.</returns>
        /// <exception cref="ArgumentException">thrown if the vector is not correctly balanced.</exception>
        internal static byte[][] SplitIntoJaggedArray(byte[] bytes, int numBytesPerArray)
        {
            if (bytes.Length % numBytesPerArray != 0)
            {
                throw new ArgumentException("Unbalanced vector.");
            }

            var result = new byte[bytes.Length / numBytesPerArray][];
            for (var i = 0; i < bytes.Length; i += numBytesPerArray)
            {
                result[i / numBytesPerArray] = bytes.Skip(i).Take(numBytesPerArray).ToArray();
            }

            return result;
        }
    }
}