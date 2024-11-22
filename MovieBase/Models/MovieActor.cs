namespace MovieBase.Models
{
    public class MovieActor
    {
        public int MovieTitleId { get; set; }
        public MovieData MovieData { get; set; } 

        public int ActorId { get; set; }
        public Actor Actor { get; set; }
    }
}
