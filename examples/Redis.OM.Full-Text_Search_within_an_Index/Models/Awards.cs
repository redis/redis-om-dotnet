

using Redis.OM.Modeling;

namespace Redis.OM.Full_Text_Search_within_an_Index.Models
{
    [Document(IndexName = "awards-idx", StorageType = StorageType.Json)]
    public class Awards
    {
        [Indexed(Sortable = true)]
        public int Wins { get; set; }
        [Indexed(Sortable = true)]
        public int Nominations { get; set; }
        [Searchable]
        public string? Text { get; set; }
    }
}
