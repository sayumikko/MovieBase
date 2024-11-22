using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieBase
{
    public class Movie
    {
        public string Title { get; set; }
        public HashSet<string> Actors { get; set; } = new HashSet<string>();
        public string Director { get; set; }
        public HashSet<string> Tags { get; set; } = new HashSet<string>();
        public double Rating { get; set; }

        public Movie(string title, double rating)
        {
            Title = title;
            Rating = rating;
        }

        public async Task LoadActorsAsync(Func<Task<HashSet<string>>> loadActorsFunc)
        {
            var actors = await loadActorsFunc();
            Actors.UnionWith(actors);
        }

        public double CalculateSimilarity(Movie other)
        {
            double similarityScore = 0.0;

            int totalActors = Actors.Count + other.Actors.Count;
            if (totalActors > 0)
            {
                double commonActorsRatio = 2.0 * Actors.Intersect(other.Actors).Count() / totalActors;
                similarityScore += commonActorsRatio * 0.25;
            }

            if (Director == other.Director)
            {
                similarityScore += 0.05;
            }

            int totalTags = Tags.Count + other.Tags.Count;
            if (totalTags > 0)
            {
                double commonTagsRatio = 2.0 * Tags.Intersect(other.Tags).Count() / totalTags;
                similarityScore += commonTagsRatio * 0.2;
            }

            similarityScore += Math.Min(Rating, other.Rating) * 0.025;

            return Math.Min(similarityScore, 1.0);
        }
    }
}
