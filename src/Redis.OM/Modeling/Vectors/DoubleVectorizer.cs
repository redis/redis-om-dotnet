using System;
using System.Linq;
using Redis.OM.Contracts;

namespace Redis.OM.Modeling.Vectors;

/// <summary>
/// A vectorizer for double arrays.
/// </summary>
public class DoubleVectorizer : IVectorizer<double[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleVectorizer"/> class.
    /// </summary>
    /// <param name="dim">The dimensions.</param>
    public DoubleVectorizer(int dim)
    {
        Dim = dim;
    }

    /// <inheritdoc />
    public VectorType VectorType => VectorType.FLOAT64;

    /// <inheritdoc />
    public int Dim { get; }

    /// <inheritdoc />
    public byte[] Vectorize(double[] obj) => obj.SelectMany(BitConverter.GetBytes).ToArray();
}