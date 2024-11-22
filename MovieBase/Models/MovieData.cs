namespace MovieBase.Models
{
    public class MovieData
    {
        public int MovieDataId { get; set; }
        public string Title { get; set; }
        public int? DirectorId { get; set; }
        public double Rating { get; set; }

        public Director Director { get; set; }
        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
        public ICollection<MovieTag> MovieTags { get; set; } = new List<MovieTag>();
        public ICollection<MovieSimilarity> SimilarMovies { get; set; }
    }
}
