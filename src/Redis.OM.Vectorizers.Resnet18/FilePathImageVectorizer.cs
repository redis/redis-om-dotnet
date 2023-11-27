using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.Resnet18;

public class FilePathImageVectorizer : IVectorizer<string>
{
    public byte[] Vectorize(string obj)
    {
        var input = new ImageInput() { ImageSource = obj };
        return Vectorize(new [] { input })[0].SelectMany(BitConverter.GetBytes).ToArray();
    }

    public VectorType VectorType => VectorType.FLOAT32;
    public int Dim => 512;

    public static Lazy<EstimatorChain<TransformerChain<ColumnCopyingTransformer>>> Pipeline = new(CreatePipeline);

    private static readonly Lazy<MLContext> MlContext = new(()=>new MLContext());

    private static EstimatorChain<TransformerChain<ColumnCopyingTransformer>> CreatePipeline()
    {
        var mlContext = MlContext.Value;
        var pipeline = mlContext.Transforms
            .LoadImages("ImageObject", "", "ImageSource")
            .Append(mlContext.Transforms.ResizeImages("ImageObject", 224, 224))
            .Append(mlContext.Transforms.ExtractPixels("Pixels", "ImageObject"))
            .Append(mlContext.Transforms.DnnFeaturizeImage("Features",
                m => m.ModelSelector.ResNet18(mlContext, m.OutputColumn, m.InputColumn), "Pixels"));

        return pipeline;
    }
        
    public static float[][] Vectorize(IEnumerable<ImageInput> images)
    {
        var mlContext = MlContext.Value;
        var dataView = mlContext.Data.LoadFromEnumerable(images);
        
        var transformedData = Pipeline.Value.Fit(dataView).Transform(dataView);
        var vector = transformedData.GetColumn<float[]>("Features").ToArray();
        return vector;
    }
}