using System;
using System.Linq;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

public class SimpleVectorizerAttribute : VectorizerAttribute<string>
{
    public override VectorType VectorType => VectorType.FLOAT32;
    public override int Dim => 30;

    public override IVectorizer<string> Vectorizer => new SimpleVectorizer();

    public override byte[] Vectorize(object obj)
    {
        if (obj is not string s)
        {
            throw new Exception("Could not vectorize non-string");
        }

        return Vectorizer.Vectorize(s);
    }
}

public class SimpleVectorizer : IVectorizer<string>
{
    public VectorType VectorType => VectorType.FLOAT32;
    public int Dim => 30;
    public byte[] Vectorize(string obj)
    {
        var floats = new float[30];
        for (var i = 0; i < 30; i++)
        {
            floats[i] = i;
        }

        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }
}