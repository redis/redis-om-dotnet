using System;
using Redis.OM.Modeling;

namespace Redis.OM
{
    /// <summary>
    /// Represents a vector created from an item.
    /// </summary>
    public abstract class Vector
    {
        /// <summary>
        /// Gets or sets the Embedding. You may set the embedding yourself, if it's not set when Redis OM inserts the vector, it will generate it for you.
        /// </summary>
        public byte[]? Embedding { get; set; }

        /// <summary>
        /// Gets the embedding represented as an array of floats.
        /// </summary>
        public float[]? Floats => Embedding is not null ? VectorUtils.VectorBytesToFloats(Embedding) : null;

        /// <summary>
        /// Gets the embedding represented as an array of doubles.
        /// </summary>
        public double[]? Doubles => Embedding is not null ? VectorUtils.VectorBytesToDoubles(Embedding) : null;

        /// <summary>
        /// Gets The object backed by this vector.
        /// </summary>
        internal abstract object? Obj { get; }

        /// <summary>
        /// Gets a vector of the type.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>A vector of the given type.</returns>
        public static Vector<T> Of<T>(T val)
            where T : class
        {
            return new Vector<T>(val);
        }

        /// <summary>
        /// Embeds the Vector using the provided vectorizer.
        /// </summary>
        /// <param name="attr">The Vectorizer.</param>
        public abstract void Embed(VectorizerAttribute attr);
    }

    /// <summary>
    /// Represents a vector created from an item of type T.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
#pragma warning disable SA1402
    public sealed class Vector<T> : Vector, IEquatable<Vector<T>>
    where T : class
#pragma warning restore SA1402
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector{T}"/> class.
        /// </summary>
        /// <param name="value">The item the vector will represent.</param>
        public Vector(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the item represented by the vector.
        /// </summary>
        public T Value { get; }

        /// <inheritdoc/>
        internal override object? Obj => Value;

        /// <summary>
        /// Embeds the Vector using the provided vectorizer.
        /// </summary>
        /// <param name="attr">The Vectorizer.</param>
        public override void Embed(VectorizerAttribute attr)
        {
            if (attr is not VectorizerAttribute<T> vectorizerAttribute)
            {
                throw new InvalidOperationException($"VectorizerAttribute must be of the type {typeof(T).Name}");
            }

            Embedding = vectorizerAttribute.Vectorizer.Vectorize(Value);
        }

        /// <inheritdoc />
        public bool Equals(Vector<T> other)
        {
            return Value == other.Value && Embedding == other.Embedding;
        }
    }
}