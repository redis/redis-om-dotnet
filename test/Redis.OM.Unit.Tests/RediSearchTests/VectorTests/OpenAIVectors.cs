using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;
using Redis.OM.Vectorizers;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class OpenAIVectors
{
    [RedisIdField]
    public string Id { get; set; }
    
    [Indexed]
    [OpenAIVectorizer]
    public Vector<string> Sentence { get; set; }

    [Indexed]
    public string Name { get; set; }

    [Indexed]
    public int Age { get; set; }

    public VectorScores VectorScore { get; set; }
}