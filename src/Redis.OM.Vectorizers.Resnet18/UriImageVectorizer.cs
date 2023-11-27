using System.Drawing;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Image;
using Redis.OM.Contracts;
using Redis.OM.Modeling;

namespace Redis.OM.Vectorizers.Resnet18;

public class UriImageVectorizer : IVectorizer<string>
{
    public byte[] Vectorize(string obj)
    {
        var imageStream = Configuration.Instance.Client.GetAsync(obj).Result.Content.ReadAsStream();
        var image = Image.FromStream(imageStream);
        var resized = new Bitmap(image, new Size(224, 224));
        var input = new InMemoryImageData() { Image = resized};
        var vector = Vectorize(new [] { input })[0].SelectMany(BitConverter.GetBytes).ToArray();
        return vector;
    }

    public VectorType VectorType => VectorType.FLOAT32;
    public int Dim => 512;

    public static Lazy<EstimatorChain<TransformerChain<ColumnCopyingTransformer>>> Pipeline = new(CreatePipeline);

    private static readonly Lazy<MLContext> MlContext = new(()=>new MLContext());

    private static EstimatorChain<TransformerChain<ColumnCopyingTransformer>> CreatePipeline()
    {
        var mlContext = MlContext.Value;
        var pipeline = mlContext.Transforms.ExtractPixels("Pixels", "Image")
            .Append(mlContext.Transforms.DnnFeaturizeImage("Features",
                m => m.ModelSelector.ResNet18(mlContext, m.OutputColumn, m.InputColumn), "Pixels"));

        return pipeline;
    }
        
    public static float[][] Vectorize(IEnumerable<InMemoryImageData> images)
    {
        var mlContext = MlContext.Value;
        var dataView = mlContext.Data.LoadFromEnumerable(images);
        var transformedData = Pipeline.Value.Fit(dataView).Transform(dataView);
        var vector = transformedData.GetColumn<float[]>("Features").ToArray();
        return vector;
    }

    public class InMemoryImageData
    {
        [ImageType(224,224)]
        public Bitmap Image;
    }

    public class InMemoryImageDataOutput
    {
        [ColumnName("Output")]
        public float[] Output { get; set; }
    }
    
}