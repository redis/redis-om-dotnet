using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;
using Redis.OM.Vectorizers.AllMiniLML6V2;
using Redis.OM.Vectorizers.Resnet18;

namespace Redis.OM.Vectorizer.Tests;

[Document(StorageType = StorageType.Json)]
public class DocWithVectors
{
    [RedisIdField]
    public string? Id { get; set; }

    [Indexed(Algorithm = VectorAlgorithm.HNSW)]
    [SentenceVectorizer]
    public Vector<string>? Sentence { get; set; }
    
    [Indexed]
    [ImageVectorizer]
    public Vector<string>? ImagePath { get; set; }

    public VectorScores? Scores { get; set; }
}