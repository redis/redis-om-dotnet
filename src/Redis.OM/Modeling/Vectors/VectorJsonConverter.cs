using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Redis.OM.Modeling.Vectors;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Converts the provided object to a json vector.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    internal class VectorJsonConverter<T> : JsonConverter<Vector<T>>
        where T : class
    {
        private readonly VectorizerAttribute<T> _vectorizerAttribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorJsonConverter{T}"/> class.
        /// </summary>
        /// <param name="attribute">the attribute that will be used for vectorization.</param>
        internal VectorJsonConverter(VectorizerAttribute<T> attribute)
        {
            _vectorizerAttribute = attribute;
        }

        /// <inheritdoc />
        public override Vector<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T res;
            reader.Read();
            byte[] embedding;
            if (_vectorizerAttribute is FloatVectorizerAttribute floatVectorizer)
            {
                float[] floats = new float[floatVectorizer.Dim];
                for (var i = 0; i < floatVectorizer.Dim; i++)
                {
                    floats[i] = reader.GetSingle();
                    reader.Read();
                }

                res = (floats as T) !;
                embedding = floats.GetBytes();
            }
            else if (_vectorizerAttribute is DoubleVectorizerAttribute doubleVectorizer)
            {
                double[] doubles = new double[doubleVectorizer.Dim];
                for (var i = 0; i < doubleVectorizer.Dim; i++)
                {
                    doubles[i] = reader.GetDouble();
                    reader.Read();
                }

                res = (doubles as T) !;
                embedding = doubles.GetBytes();
            }
            else
            {
                reader.Read();
                res = JsonSerializer.Deserialize<T>(reader.GetString() !) !;
                reader.Read();
                reader.Read(); // Vector
                reader.Read(); // start array
                if (_vectorizerAttribute.VectorType == VectorType.FLOAT32)
                {
                    var floats = new float[_vectorizerAttribute.Dim];
                    for (var i = 0; i < _vectorizerAttribute.Dim; i++)
                    {
                        floats[i] = reader.GetSingle();
                        reader.Read(); // each item
                    }

                    embedding = floats.GetBytes();
                }
                else
                {
                    var doubles = new double[_vectorizerAttribute.Dim];
                    for (var i = 0; i < _vectorizerAttribute.Dim; i++)
                    {
                        doubles[i] = reader.GetDouble();
                        reader.Read(); // each item
                    }

                    embedding = doubles.GetBytes();
                }

                reader.Read(); // end array
            }

            var vector = new Vector<T>(res!) { Embedding = embedding };
            return vector;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Vector<T> value, JsonSerializerOptions options)
        {
            if (_vectorizerAttribute is DoubleVectorizerAttribute && value is Vector<double[]> doubleVector)
            {
                writer.WriteStartArray();
                foreach (var d in doubleVector.Value)
                {
                    writer.WriteNumberValue(d);
                }

                writer.WriteEndArray();
                return;
            }

            if (_vectorizerAttribute is FloatVectorizerAttribute && value is Vector<double[]> floatVector)
            {
                writer.WriteStartArray();
                foreach (var d in floatVector.Value)
                {
                    writer.WriteNumberValue(d);
                }

                writer.WriteEndArray();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            writer.WriteStringValue(JsonSerializer.Serialize(value.Obj));
            if (value.Embedding is null)
            {
                value.Embed(_vectorizerAttribute);
            }

            var bytes = value.Embedding!;
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
                var doubles = jagged.Select(x => BitConverter.ToDouble(x, 0)).ToArray();
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