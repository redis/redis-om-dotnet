using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers;

/// <inheritdoc />
public class AzureOpenAIVectorizerAttribute : VectorizerAttribute<string>
{
    /// <inheritdoc />
    public AzureOpenAIVectorizerAttribute(string deploymentName, string resourceName, int dim)
    {
        DeploymentName = deploymentName;
        ResourceName = resourceName;
        Dim = dim;
        Vectorizer = new AzureOpenAIVectorizer(Configuration.Instance.AzureOpenAIApiKey, ResourceName, DeploymentName, Dim);
    }

    /// <summary>
    /// Gets the DeploymentName.
    /// </summary>
    public string DeploymentName { get; }

    /// <summary>
    /// Gets the resource name.
    /// </summary>
    public string ResourceName { get; }

    /// <inheritdoc />
    public override IVectorizer<string> Vectorizer { get; }

    /// <inheritdoc />
    public override VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public override int Dim { get; }

    /// <inheritdoc />
    public override byte[] Vectorize(object obj)
    {
        if (obj is not string s)
        {
            throw new ArgumentException("Object must be a string to be embedded", nameof(obj));
        }
        
        var floats = AzureOpenAIVectorizer.GetFloats(s, ResourceName, DeploymentName, Configuration.Instance.AzureOpenAIApiKey);
        return floats.SelectMany(BitConverter.GetBytes).ToArray();
    }
}