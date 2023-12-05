using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Runtime;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Onnx;

namespace Redis.OM.Vectorizers.Resnet18;

/// <summary>
/// Extensions pulled and slightly modified from  from ML.NET to service this package as the content files cannot be
/// reliably copied from transitive dependencies. 
/// </summary>
internal static class DnnImageModelSelectorExtensions
{
    /// <summary>
    /// Returns an estimator chain with the two corresponding models (a preprocessing one and a main one) required for the ResNet pipeline.
    /// Also includes the renaming ColumnsCopyingTransforms required to be able to use arbitrary input and output column names.
    /// This assumes both of the models are in the same location as the file containing this method, which they will be if used through the NuGet.
    /// This should be the default way to use ResNet18 if importing the model from a NuGet.
    /// </summary>
    public static EstimatorChain<ColumnCopyingTransformer> ResNet18(this DnnImageModelSelector dnnModelContext, IHostEnvironment env, string outputColumnName, string inputColumnName, MLContext context)
    {
        return ResNet18(dnnModelContext, env, outputColumnName, inputColumnName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"), context);
    }

    /// <summary>
    /// This allows a custom model location to be specified. This is useful is a custom model is specified,
    /// or if the model is desired to be placed or shipped separately in a different folder from the main application. Note that because ONNX models
    /// must be in a directory all by themselves for the OnnxTransformer to work, this method appends a ResNet18Onnx/ResNetPrepOnnx subdirectory
    /// to the passed in directory to prevent having to make that directory manually each time.
    /// </summary>
    public static EstimatorChain<ColumnCopyingTransformer> ResNet18(this DnnImageModelSelector dnnModelContext, IHostEnvironment env, string outputColumnName, string inputColumnName, string modelDir, MLContext context)
    {
        var modelChain = new EstimatorChain<ColumnCopyingTransformer>();

        var inputRename = context.Transforms.CopyColumns("OriginalInput", inputColumnName);
        var midRename = context.Transforms.CopyColumns("Input247", "PreprocessedInput");
        var endRename = context.Transforms.CopyColumns(outputColumnName, "Pooling395_Output_0");

        // There are two estimators created below. The first one is for image preprocessing and the second one is the actual DNN model.
        var prepEstimator = context.Transforms.ApplyOnnxModel("PreprocessedInput", "OriginalInput", Path.Combine(modelDir, "ResNetPrepOnnx", "ResNetPreprocess.onnx"));
        var mainEstimator = context.Transforms.ApplyOnnxModel("Pooling395_Output_0", "Input247", Path.Combine(modelDir, "ResNet18Onnx", "ResNet18.onnx"));
        modelChain = modelChain.Append(inputRename);
        var modelChain2 = modelChain.Append(prepEstimator);
        modelChain = modelChain2.Append(midRename);
        modelChain2 = modelChain.Append(mainEstimator);
        modelChain = modelChain2.Append(endRename);
        return modelChain;
    }
}