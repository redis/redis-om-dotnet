using System;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling
{
    /// <summary>
    /// Attribute to decorate vector score field. A field decorated with this will have the sentinel value -1 when
    /// the score is not present in the result.
    /// </summary>
    public class KnnVectorScore : JsonConverterAttribute
    {
        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            return new JsonScoreConverter();
        }
    }
}