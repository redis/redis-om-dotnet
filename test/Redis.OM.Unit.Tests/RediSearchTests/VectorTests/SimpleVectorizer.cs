using System;
using System.Linq;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

public class SimpleVectorizer : VectorizerAttribute
{
    public override VectorType VectorType => VectorType.FLOAT32;
    public override int Dim => 30;

    public override byte[] Vectorize(object obj)
    {
        var floats = new float[30];
        for (var i = 0; i < 30; i++)
        {
            floats[i] = i;
        }

        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }
}