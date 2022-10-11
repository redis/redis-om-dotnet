using Redis.OM.Modeling;
using System.Text.Json.Serialization;

namespace Redis.OM.FullTextSearch.Models
{
    [Document(StorageType = StorageType.Json, IndexName = "movie-idx")]
    public class Movie
    {
        [Searchable]
        public string Title { get; set; }
        [Indexed(Sortable = true)]
        public long Year { get; set; } 
        [Indexed]
        public string? Plot { get; set; }
        [Indexed]
        public List<string> Languages { get; set; }
        [Indexed(Sortable = true)]
        public DateTime Released { get; set; } 
        [Indexed]
        public List<string> Cast { get; set; }
        [Indexed]
        public List<string> Directors { get; set; }
        [Indexed]
        public List<string> Writers { get; set; }
        [Indexed(Sortable = true)]
        public int Runtime { get; set; }
        [Indexed]
        public List<Genres>  Genres { get; set; }
        [Indexed(CascadeDepth = 2)]
        public Awards Awards { get; set; }
        [Indexed(Sortable = true)]
        public double? ImdbRating { get; set; }
        [Indexed]
        public Country Country { get; set; }

    }
    public enum Country
    {
        UK,
        USA,
        IND,
        KR
    }
}
