using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.Resnet18;

/// <summary>
/// A Vectorizer that uses Resnet 18 to perform vectorization. It accepts either a file path or full URI to an image as
/// input and vectorizers the inputs returning a Float32 vector with a dimensionality of 512
/// </summary>
public class ImageVectorizer : IVectorizer<string>
{
    /// <inheritdoc />
    public VectorType VectorType => VectorType.FLOAT32;

    /// <inheritdoc />
    public int Dim => 512;
    
    /// <inheritdoc />
    public byte[] Vectorize(string obj)
    {
        var isUri = Uri.TryCreate(obj, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (isUri)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = uri,
            };
            var imageStream = Configuration.Instance.Client.Send(request).Content.ReadAsStream();
            var image = MLImage.CreateFromStream(imageStream);
            var vector = VectorizeImages(new [] { image })[0].SelectMany(BitConverter.GetBytes).ToArray();
            return vector;
        }

        if (!File.Exists(obj))
        {
            throw new ArgumentException(
                $"Input {obj} was not a well formed URI, and was not a file path that exists on this system.", nameof(obj));
        }

        return VectorizeFiles(new[] { obj })[0].SelectMany(BitConverter.GetBytes).ToArray();
    }

    private static readonly Lazy<EstimatorChain<TransformerChain<ColumnCopyingTransformer>>> FilePipeline = new(CreateFilePipeline);

    private static readonly Lazy<MLContext> MlContext = new(()=>new MLContext());

    private static EstimatorChain<TransformerChain<ColumnCopyingTransformer>> CreateFilePipeline()
    {
        var mlContext = MlContext.Value;
        var pipeline = mlContext.Transforms
            .LoadImages("ImageObject", "", "ImageSource")
            .Append(mlContext.Transforms.ResizeImages("ImageObject", 224, 224))
            .Append(mlContext.Transforms.ExtractPixels("Pixels", "ImageObject"))
            .Append(mlContext.Transforms.DnnFeaturizeImage("Features",
                m => m.ModelSelector.ResNet18(mlContext, m.OutputColumn, m.InputColumn, mlContext), "Pixels"));

        return pipeline;
    }
    
    /// <summary>
    /// Vectorizers a series of image file paths.
    /// </summary>
    /// <param name="imagePaths"></param>
    /// <returns></returns>
    public static float[][] VectorizeFiles(IEnumerable<string> imagePaths)
    {
        var images = imagePaths.Select(x => new ImageInput(x));
        var mlContext = MlContext.Value;
        var dataView = mlContext.Data.LoadFromEnumerable(images);
        
        var transformedData = FilePipeline.Value.Fit(dataView).Transform(dataView);
        var vector = transformedData.GetColumn<float[]>("Features").ToArray();
        return vector;
    }

    private static readonly Lazy<EstimatorChain<TransformerChain<ColumnCopyingTransformer>>> BitmapPipeline = new(CreateBitmapPipeline);

    private static EstimatorChain<TransformerChain<ColumnCopyingTransformer>> CreateBitmapPipeline()
    {
        var mlContext = MlContext.Value;
        var pipeline = mlContext.Transforms
            .ResizeImages("Image", 224,224)
            .Append(mlContext.Transforms.ExtractPixels("Pixels", "Image"))
            .Append(mlContext.Transforms.DnnFeaturizeImage("Features",
                m => m.ModelSelector.ResNet18(mlContext, m.OutputColumn, m.InputColumn, mlContext), "Pixels"));

        return pipeline;
    }
    
    /// <summary>
    /// Encodes a collection of images.
    /// </summary>
    /// <param name="mlImages"></param>
    /// <returns></returns>
    public static float[][] VectorizeImages(IEnumerable<MLImage> mlImages)
    {
        var images = mlImages.Select(x => new InMemoryImageData(x));
        var mlContext = MlContext.Value;
        var dataView = mlContext.Data.LoadFromEnumerable(images);
        var transformedData = BitmapPipeline.Value.Fit(dataView).Transform(dataView);
        var vector = transformedData.GetColumn<float[]>("Features").ToArray();
        return vector;
    }
}