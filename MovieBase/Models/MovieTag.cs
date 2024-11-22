namespace MovieBase.Models
{
    public class MovieTag
    {
        public int MovieTitleId { get; set; }
        public MovieData MovieData { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
