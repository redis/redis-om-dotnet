using System;
using Redis.OM.Modeling;
using Redis.OM.Vectorizers;

namespace Redis.OM.Unit.Tests;

[Document(StorageType = StorageType.Json)]
public class OpenAICompletionResponse
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed(DistanceMetric = DistanceMetric.COSINE, Algorithm = VectorAlgorithm.HNSW, M = 16)]
    [OpenAIVectorizer]
    public Vector<string> Prompt { get; set; }

    public string Response { get; set; }

    [Indexed]
    public string Language { get; set; }
    
    [Indexed]
    public DateTime TimeStamp { get; set; }
}