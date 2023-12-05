using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Redis.OM.Modeling.Vectors
{
    /// <summary>
    /// A collector for vector scores, binding this to your model causes Redis OM to bind all scores resulting from
    /// a vector query to it. Otherwise it will be ignored when it is added to Redis.
    /// </summary>
    public class VectorScores
    {
        /// <summary>
        /// The Range score suffix.
        /// </summary>
        internal const string RangeScoreSuffix = "_RangeScore";

        /// <summary>
        /// The Nearest neighbor score name.
        /// </summary>
        internal const string NearestNeighborScoreName = "KnnNeighborScore";

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorScores"/> class.
        /// </summary>
        internal VectorScores()
        {
        }

        /// <summary>
        /// Gets the nearest neighbor score.
        /// </summary>
        [JsonIgnore]
        public double? NearestNeighborsScore { get; internal set; }

        /// <summary>
        /// Gets the first score from the vector ranges.
        /// </summary>
        [JsonIgnore]
        public double? RangeScore => RangeScores.FirstOrDefault().Value;

        /// <summary>
        /// Gets or sets the range score dictionary.
        /// </summary>
        [JsonIgnore]
        internal Dictionary<string, double> RangeScores { get; set; } = new ();
    }
}