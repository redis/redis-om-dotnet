using Redis.OM.Vectorizers;
using Redis.OM.Modeling;
using Redis.OM.Modeling.Vectors;

namespace Redis.OM.Unit.Tests;

[Document]
public class HuggingFaceVectors
{
    [RedisIdField]
    public string Id { get; set; }
    
    [Indexed]
    [HuggingFaceVectorizer(ModelId = "sentence-transformers/all-MiniLM-L6-v2")]
    public Vector<string> Sentence { get; set; }

    [Indexed]
    public string Name { get; set; }

    [Indexed]
    public int Age { get; set; }

    public VectorScores VectorScore { get; set; }
}