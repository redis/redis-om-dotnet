using System.Net.Http.Json;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

public class HuggingFaceApiSentenceVectorizerAttribute : VectorizerAttribute
{
    public string? ModelId { get; set; }
    public override VectorType VectorType => VectorType.FLOAT32;
    private int? _dim;

    public override int Dim
    {
        get
        {
            if (_dim is not null) return _dim.Value;
            const string testString = "This is a vector dimensionality probing query";
            var floats = GetFloats(testString);
            _dim = floats.Length;

            return _dim.Value;
        }
    }

    public override byte[] Vectorize(object obj)
    {
        if (obj is not string s)
        {
            throw new ArgumentException("Object must be a string", nameof(obj));
        }
        
        var floats = GetFloats(s);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }

    public float[] GetFloats(string s)
    {
        var modelId = ModelId ?? Configuration.Instance["REDIS_OM_HF_MODEL_ID"];
        if (modelId is null) throw new InvalidOperationException("Model Id Required to use Hugging Face API.");
        return HuggingFaceApiSentenceVectorizer.GetFloats(s, modelId, Configuration.Instance.HuggingFaceAuthorizationToken);
    }
}