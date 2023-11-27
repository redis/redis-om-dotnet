using Microsoft.ML.Data;

namespace Redis.OM.Vectorizers.Resnet18;

public class ImageInput
{
    [ColumnName(@"ImageSource")]
    public string ImageSource { get; set; }
}