using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;

namespace Redis.OM.Vectorizers.Resnet18;

internal class ImageInput
{
    [ColumnName(@"ImageSource")]
    public string ImageSource { get; set; }

    public ImageInput(string imageSource)
    {
        ImageSource = imageSource;
    }
}

internal class InMemoryImageData
{
    [ImageType(224,224)]
    public MLImage Image;

    public InMemoryImageData(MLImage image)
    {
        Image = image;
    }
}