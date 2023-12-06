using System;
using System.Linq;
using System.Text;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Xunit;

namespace Redis.OM.Unit.Tests;

[Collection("Redis")]
public class VectorFunctionalTests
{
    private readonly IRedisConnection _connection;

    public VectorFunctionalTests(RedisSetup setup)
    {
        _connection = setup.Connection;
    }

    [SkipIfMissingEnvVar("REDIS_OM_HF_TOKEN")]
    public void TestHuggingFaceVectorizer()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(HuggingFaceVectors));
        _connection.CreateIndex(typeof(HuggingFaceVectors));
        var collection = new RedisCollection<HuggingFaceVectors>(_connection);
        var sentenceVector = Vector.Of("Hello World this is Hal.");
        var obj = new HuggingFaceVectors
        {
            Age = 45,
            Sentence = sentenceVector,
            Name = "Hal"
        };

        collection.Insert(obj);
        var queryVector = Vector.Of("Hello World this is Hal.");
        var res = collection.NearestNeighbors(x => x.Sentence, 2, queryVector).First();
        Assert.Equal(obj.Id, res.Id);
        Assert.Equal(0, res.VectorScore.NearestNeighborsScore);
        Assert.Equal(obj.Sentence.Value, res.Sentence.Value);
    }

    [SkipIfMissingEnvVar("REDIS_OM_HF_TOKEN")]
    public void TestParis()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(HuggingFaceVectors));
        _connection.CreateIndex(typeof(HuggingFaceVectors));
        var collection = new RedisCollection<HuggingFaceVectors>(_connection);
        var sentenceVector = Vector.Of("What is the capital of France?");
        var obj = new HuggingFaceVectors
        {
            Age = 2259,
            Sentence = sentenceVector,
            Name = "Paris"
        };

        collection.Insert(obj);
        var queryVector = Vector.Of("What really is the capital of France?");
        var res = collection
            .First(x => x.Sentence.VectorRange(queryVector, .1, "range") && x.Age > 1000);
        res = collection.NearestNeighbors(x => x.Sentence, 2, queryVector).First(x => x.Age > 1000);
        Assert.Equal(obj.Id, res.Id);
        Assert.True(res.VectorScore.RangeScore < .1);
        Assert.Equal(sentenceVector.Value, res.Sentence.Value);
        Assert.Equal(obj.Sentence.Embedding, res.Sentence.Embedding);
    }

    [SkipIfMissingEnvVar("REDIS_OM_OAI_TOKEN")]
    public void TestOpenAIVectorizer()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(OpenAIVectors));
        _connection.CreateIndex(typeof(OpenAIVectors));
        var collection = new RedisCollection<OpenAIVectors>(_connection);
        var sentenceVector = Vector.Of("Hello World this is Hal."); 
        var obj = new OpenAIVectors
        {
            Age = 45,
            Sentence = sentenceVector,
            Name = "Hal"
        };

        collection.Insert(obj);
        var queryVector = Vector.Of("Hello World this is Hal.");
        var res = collection.NearestNeighbors(x => x.Sentence, 2, queryVector).First();
        Assert.Equal(obj.Id, res.Id);
        Assert.True(res.VectorScore.NearestNeighborsScore < .01);
        Assert.Equal(obj.Sentence.Value, res.Sentence.Value);
        Assert.Equal(obj.Sentence.Embedding, res.Sentence.Embedding);
    }

    [SkipIfMissingEnvVar("REDIS_OM_OAI_TOKEN")]
    public void TestOpenAIVectorRange()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(OpenAIVectors));
        _connection.CreateIndex(typeof(OpenAIVectors));
        var collection = new RedisCollection<OpenAIVectors>(_connection);
        var sentenceVector = Vector.Of("What is the capital of France?");
        var obj = new OpenAIVectors
        {
            Age = 2259,
            Sentence = sentenceVector,
            Name = "Paris"
        };

        collection.Insert(obj);
        var queryVector = Vector.Of("What really is the capital of France?");
        var res = collection.First(x => x.Sentence.VectorRange(queryVector, 1, "range"));
        Assert.Equal(obj.Id, res.Id);
        Assert.True(res.VectorScore.RangeScore < .1);
        Assert.Equal(obj.Sentence.Value, res.Sentence.Value);
        Assert.Equal(obj.Sentence.Embedding, res.Sentence.Embedding);
    }
    
    [Fact]
    public void BasicRangeQuery()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("FooBarBaz");
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        });
        var queryVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var res = collection.First(x => x.SimpleHnswVector.VectorRange(queryVector, 5));
        Assert.Equal("helloWorld", res.Id);
    }

    [Fact]
    public void MultiRangeOnSameProperty()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("FooBarBaz");
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        });
        var queryVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var res = collection.First(x => x.SimpleHnswVector.VectorRange(queryVector, 5) && x.SimpleHnswVector.VectorRange(queryVector, 6));
        Assert.Equal("helloWorld", res.Id);
    }

    [Fact]
    public void RangeAndKnn()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("FooBarBaz");
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        });
        var queryVector =Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
        queryVector[0] += 2;
        var stringQueryVector = Vector.Of("FooBarBaz");
        var res = collection.NearestNeighbors(x=>x.SimpleVectorizedVector, 1, stringQueryVector)
            .First(x => x.SimpleHnswVector.VectorRange(queryVector, 5, "range"));
        Assert.Equal("helloWorld", res.Id);
        Assert.Equal(4, res.VectorScores.RangeScore);
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }
    
    [Fact]
    public void RangeAndKnnWithVector()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("FooBarBaz");
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        });
        var queryVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        queryVector.Value[0] += 2;
        var stringQueryVector = Vector.Of("FooBarBaz");
        var res = collection.NearestNeighbors(x=>x.SimpleVectorizedVector, 1, stringQueryVector)
            .First(x => x.SimpleHnswVector.VectorRange(queryVector, 5, "range"));
        Assert.Equal("helloWorld", res.Id);
        Assert.Equal(4, res.VectorScores.RangeScore);
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }

    [Fact]
    public void BasicQuery()
    {
        _connection.CreateIndex(typeof(ObjectWithVector));
        var collection = new RedisCollection<ObjectWithVector>(_connection);
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("FooBarBaz");
        collection.Insert(new ObjectWithVector
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        });
        var queryVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        queryVector.Value[0] += 2;

        var stringQueryVector = Vector.Of("FooBarBaz");
        var res = collection.NearestNeighbors(x => x.SimpleVectorizedVector, 1, stringQueryVector).First();
        Assert.Equal("helloWorld", res.Id);
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
        res = collection.NearestNeighbors(x => x.SimpleHnswVector, 1, queryVector).First();
        Assert.Equal(4, res.VectorScores.NearestNeighborsScore);
    }

    [Fact]
    public void ScoresOnHash()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(ObjectWithVectorHash));
        _connection.CreateIndex(typeof(ObjectWithVectorHash));
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("foo");
        var obj = new ObjectWithVectorHash
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector,
        };
        var collection = new RedisCollection<ObjectWithVectorHash>(_connection);
        collection.Insert(obj);
        var res = collection.NearestNeighbors(x => x.SimpleHnswVector, 5, simpleHnswVector).First();
        
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }
    
    [Fact]
    public void ScoresOnHashVectorizer()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(ObjectWithVectorHash));
        _connection.CreateIndex(typeof(ObjectWithVectorHash));
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("foo");
        var obj = new ObjectWithVectorHash
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector,
        };
        var collection = new RedisCollection<ObjectWithVectorHash>(_connection);
        collection.Insert(obj);
        var res = collection.NearestNeighbors(x => x.SimpleVectorizedVector, 5, "foo").First();

        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }

    [Fact]
    public void HybridQueryTest()
    {
        _connection.DropIndexAndAssociatedRecords(typeof(ObjectWithVectorHash));
        _connection.CreateIndex(typeof(ObjectWithVectorHash));
        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("foo");
        var obj = new ObjectWithVectorHash
        {
            Id = "theOneWithStuff",
            SimpleHnswVector = simpleHnswVector,
            Name = "Steve",
            Num = 6,
            SimpleVectorizedVector = simpleVectorizedVector,
        };
        var collection = new RedisCollection<ObjectWithVectorHash>(_connection);
        collection.Insert(obj);
        var res = collection.Where(x=>x.Name == "Steve" && x.Num == 6).NearestNeighbors(x => x.SimpleHnswVector, 5, simpleHnswVector).First();
        
        Assert.Equal(0, res.VectorScores.NearestNeighborsScore);
    }

    [Fact]
    public void TestIndex()
    {
        _connection.CreateIndex(typeof(ObjectWithVectorHash));

        var simpleHnswVector = Vector.Of(Enumerable.Range(0, 10).Select(x => (double)x).ToArray());
        var simpleVectorizedVector = Vector.Of("foo");
        var obj = new ObjectWithVectorHash
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector,
        };
        
        var key = _connection.Set(obj);
        var res = _connection.Get<ObjectWithVectorHash>(key);
        Assert.Equal(simpleHnswVector.Value, res.SimpleHnswVector.Value);
        Assert.Equal(simpleHnswVector.Embedding, res.SimpleHnswVector.Embedding);

        simpleVectorizedVector = Vector.Of("foobarbaz");
        key = _connection.Set(new ObjectWithVector()
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        });

        var jsonRes = _connection.Get<ObjectWithVector>(key);
        
        Assert.Equal(simpleHnswVector.Value, jsonRes.SimpleHnswVector.Value);
        Assert.Equal(simpleHnswVector.Embedding, jsonRes.SimpleHnswVector.Embedding);
        Assert.Equal(simpleVectorizedVector.Value, jsonRes.SimpleVectorizedVector.Value);
        Assert.Equal(simpleVectorizedVector.Embedding, jsonRes.SimpleVectorizedVector.Embedding);
    }

    [Fact]
    public void Insert()
    {
        var simpleHnswJsonStr = new StringBuilder();
        var vectorizedFlatVectorJsonStr = new StringBuilder();
        simpleHnswJsonStr.Append('[');
        vectorizedFlatVectorJsonStr.Append('[');
        var simpleHnswHash = new double[10];
        var vectorizedFlatHashVector = new float[30];
        for (var i = 0; i < 10; i++)
        {
            simpleHnswHash[i] = i;
        }

        for (var i = 0; i < 30; i++)
        {
            vectorizedFlatHashVector[i] = i;
        }

        simpleHnswJsonStr.Append(string.Join(',', simpleHnswHash));
        vectorizedFlatVectorJsonStr.Append(string.Join(',', vectorizedFlatHashVector));
        simpleHnswJsonStr.Append(']');
        vectorizedFlatVectorJsonStr.Append(']');

        var simpleHnswVector = Vector.Of(simpleHnswHash);
        var simpleVectorizedVector = Vector.Of("foobar");
        var hashObj = new ObjectWithVectorHash()
        {
            Id = "helloWorld",
            SimpleHnswVector = simpleHnswVector,
            SimpleVectorizedVector = simpleVectorizedVector
        };

        var key = _connection.Set(hashObj);
        var res = _connection.Get<ObjectWithVectorHash>(key);
        Assert.Equal(simpleVectorizedVector.Value, res.SimpleVectorizedVector.Value);
        Assert.Equal(simpleVectorizedVector.Embedding, res.SimpleVectorizedVector.Embedding);
    }

    [SkipIfMissingEnvVar("REDIS_OM_OAI_TOKEN")]
    public void OpenAIQueryTest()
    {
        var provider = new RedisConnectionProvider("");

        provider.RedisCollection<OpenAICompletionResponse>();
        _connection.DropIndexAndAssociatedRecords(typeof(OpenAICompletionResponse));
        _connection.CreateIndex(typeof(OpenAICompletionResponse));
        
        var collection = new RedisCollection<OpenAICompletionResponse>(_connection);
        var query = new OpenAICompletionResponse
        {
            Language = "en_us", 
            Prompt = Vector.Of("What is the Capital of France?"), 
            Response = "Paris", 
            TimeStamp = DateTime.Now - TimeSpan.FromHours(3)
        };
        collection.Insert(query);
        var queryPrompt ="What really is the Capital of France?";
        var result = collection.First(x => x.Prompt.VectorRange(queryPrompt, .15));
        
        Assert.Equal("Paris", result.Response);

        result = collection.NearestNeighbors(x => x.Prompt, 1, queryPrompt).First();
        Assert.Equal("Paris", result.Response);
        
        result = collection.Where(x=>x.Language == "en_us").NearestNeighbors(x => x.Prompt, 1, queryPrompt).First();
        Assert.Equal("Paris", result.Response);
        
        result = collection.First(x=>x.Language == "en_us" && x.Prompt.VectorRange(queryPrompt, .15));
        Assert.Equal("Paris", result.Response);

        var ts = DateTimeOffset.Now - TimeSpan.FromHours(4);
        result = collection.First(x=>x.TimeStamp > ts && x.Prompt.VectorRange(queryPrompt, .15));
        Assert.Equal("Paris", result.Response);
    }
}