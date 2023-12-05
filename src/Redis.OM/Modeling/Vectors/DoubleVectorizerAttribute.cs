using System;
using Redis.OM.Contracts;

namespace Redis.OM.Modeling.Vectors;

/// <summary>
/// A vectorizer attribute for doubles.
/// </summary>
public class DoubleVectorizerAttribute : VectorizerAttribute<double[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleVectorizerAttribute"/> class.
    /// </summary>
    /// <param name="dim">the dimensions.</param>
    public DoubleVectorizerAttribute(int dim)
    {
        Dim = dim;
        Vectorizer = new DoubleVectorizer(dim);
    }

    /// <inheritdoc />
    public override IVectorizer<double[]> Vectorizer { get; }

    /// <inheritdoc />
    public override VectorType VectorType => VectorType.FLOAT64;

    /// <inheritdoc />
    public override int Dim { get; }

    /// <inheritdoc />
    public override byte[] Vectorize(object obj)
    {
        if (obj is not double[] doubles)
        {
            throw new InvalidOperationException("Provided object for vectorization must be a double[]");
        }

        return Vectorizer.Vectorize(doubles);
    }
}