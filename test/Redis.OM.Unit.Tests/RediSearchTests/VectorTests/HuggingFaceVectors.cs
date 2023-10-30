using Redis.OM.Vectorizers;
using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class HuggingFaceVectors
{
    [RedisIdField]
    public string Id { get; set; }
    
    [Vector]
    [HuggingFaceApiSentenceVectorizer(ModelId = "sentence-transformers/all-MiniLM-L6-v2")]
    public string Sentence { get; set; }

    [Indexed]
    public string Name { get; set; }

    [Indexed]
    public int Age { get; set; }

    public VectorScores VectorScore { get; set; }
}