using System.Drawing;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;

namespace Redis.OM.Vectorizers.Resnet18;

public class ImageInput
{
    [ColumnName(@"ImageSource")]
    public string ImageSource { get; set; }
}

public class InMemoryImageData
{
    [ImageType(224,224)]
    public Bitmap Image;
}