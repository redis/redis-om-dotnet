using System;
using System.Linq;
using Redis.OM.Contracts;

namespace Redis.OM.Modeling.Vectors;

/// <inheritdoc />
public class FloatVectorizerAttribute : VectorizerAttribute<float[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatVectorizerAttribute"/> class.
    /// </summary>
    /// <param name="dim">The dimensions of the vector.</param>
    public FloatVectorizerAttribute(int dim)
    {
        Dim = dim;
        Vectorizer = new FloatVectorizer(dim);
    }

    /// <inheritdoc />
    public override VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public override int Dim { get; }

    /// <inheritdoc/>
    public override IVectorizer<float[]> Vectorizer { get; }

    /// <inheritdoc />
    public override byte[] Vectorize(object obj)
    {
        if (obj is not float[] floats)
        {
            throw new InvalidOperationException("Must pass in an array of floats");
        }

        return Vectorizer.Vectorize(floats);
    }
}