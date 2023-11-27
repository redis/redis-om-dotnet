using Redis.OM.Modeling;
using Redis.OM.Vectorizers.AllMiniLML6V2;
using Redis.OM.Vectorizers.Resnet18;

namespace Redis.OM.Vectorizer.Tests;

[Document(StorageType = StorageType.Json)]
public class DocWithVector
{
    [RedisIdField]
    public string Id { get; set; }

    [Indexed]
    [SentenceVectorizer]
    public Vector<string> Sentence { get; set; }
    
    [Indexed]
    [UriImageVectorizer]
    public Vector<string> ImageUri { get; set; }
}