namespace MovieBase.Models
{
    public class MovieSimilarity
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public int SimilarMovieId { get; set; }
        public double SimilarityScore { get; set; }

        public MovieData Movie { get; set; }
        public MovieData SimilarMovie { get; set; }
    }
}
