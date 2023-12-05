using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithVector
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed] public string Name { get; set; }

    [Indexed] public int Num { get; set; }
    
    [Indexed(Algorithm = VectorAlgorithm.HNSW)]
    [DoubleVectorizer(10)]
    public Vector<double[]> SimpleHnswVector { get; set; }

    [Indexed(Algorithm = VectorAlgorithm.FLAT)]
    [SimpleVectorizer]
    public Vector<string> SimpleVectorizedVector { get; set; }

    public VectorScores VectorScores { get; set; }
}

[Document(StorageType = StorageType.Hash)]
public class ObjectWithVectorHash
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed] public string Name { get; set; }
    
    [Indexed] public int Num { get; set; }

    [Indexed(Algorithm = VectorAlgorithm.HNSW)]
    [DoubleVectorizer(10)]
    public Vector<double[]> SimpleHnswVector { get; set; }
    
    [Indexed(Algorithm = VectorAlgorithm.FLAT)]
    [SimpleVectorizer]
    public Vector<string> SimpleVectorizedVector { get; set; }

    public VectorScores VectorScores { get; set; }
}

[Document(StorageType = StorageType.Json, Prefixes = new []{"Simple"})]
public class ToyVector
{
    [RedisIdField] public string Id { get; set; }
    [Indexed][DoubleVectorizer(6)]public Vector<double[]> SimpleVector { get; set; }
}