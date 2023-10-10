using Redis.OM.Modeling;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class ObjectWithVector
{
    [RedisIdField]
    public string Id { get; set; }

    [Vector(Algorithm = VectorAlgorithm.HNSW, Dim = 10)]
    public double[] SimpleHnswVector { get; set; }

    [Vector(Algorithm = VectorAlgorithm.FLAT)]
    [SimpleVectorizer]
    public string SimpleVectorizedVector { get; set; }
}

[Document(StorageType = StorageType.Hash)]
public class ObjectWithVectorHash
{
    [RedisIdField]
    public string Id { get; set; }

    [Vector(Algorithm = VectorAlgorithm.HNSW, Dim = 10)]
    public double[] SimpleHnswVector { get; set; }
    
    [Vector(Algorithm = VectorAlgorithm.FLAT)]
    [SimpleVectorizer]
    public string SimpleVectorizedVector { get; set; }
}

[Document]
public class ToyVector
{
    [RedisIdField] public string Id { get; set; }
    [Vector(Dim=6)]public double[] SimpleVector { get; set; }
}