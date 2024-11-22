namespace MovieBase.Models
{
    public class Director
    {
        public int DirectorId { get; set; }
        public string Name { get; set; }

        public ICollection<MovieData> MoviesDirected { get; set; } = new List<MovieData>();
    }
}
