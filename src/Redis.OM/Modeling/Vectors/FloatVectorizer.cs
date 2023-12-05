using System;
using System.Linq;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM;

/// <summary>
/// A vectorizer for float arrays.
/// </summary>
public class FloatVectorizer : IVectorizer<float[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatVectorizer"/> class.
    /// </summary>
    /// <param name="dim">The dimensions.</param>
    public FloatVectorizer(int dim)
    {
        Dim = dim;
    }

    /// <inheritdoc />
    public VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public int Dim { get; }

    /// <inheritdoc />
    public byte[] Vectorize(float[] obj) => obj.SelectMany(BitConverter.GetBytes).ToArray();
}