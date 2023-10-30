using System.Net.Http.Json;
using System.Text.Json;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

public class AzureOpenAISentenceVectorizerAttribute : VectorizerAttribute
{
    public AzureOpenAISentenceVectorizerAttribute(string deploymentName, string resourceName, int dim)
    {
        DeploymentName = deploymentName;
        ResourceName = resourceName;
        Dim = dim;
    }

    public string DeploymentName { get; }
    public string ResourceName { get; }
    public override VectorType VectorType => VectorType.FLOAT32;
    public override int Dim { get; }
    public override byte[] Vectorize(object obj)
    {
        if (obj is not string s)
        {
            throw new ArgumentException("Object must be a string to be embedded", nameof(obj));
        }
        
        var floats = AzureOpenAISentenceVectorizer.GetFloats(s, ResourceName, DeploymentName, Configuration.Instance.AzureOpenAIApiKey);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }
}