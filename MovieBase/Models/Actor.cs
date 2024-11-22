namespace MovieBase.Models
{
    public class Actor
    {
        public int ActorId { get; set; }
        public string Name { get; set; }

        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }
}
