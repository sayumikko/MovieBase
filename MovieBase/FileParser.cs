using System.Globalization;

namespace MovieBase
{
    public class FileParser
    {
        static Dictionary<string, Movie> MoviesDictionary = new Dictionary<string, Movie>();
        static Dictionary<string, HashSet<Movie>> ActorsDirectorsDictionary = new Dictionary<string, HashSet<Movie>>();
        static Dictionary<string, HashSet<Movie>> TagsDictionary = new Dictionary<string, HashSet<Movie>>();

        static Dictionary<string, double> RatingsData = new Dictionary<string, double>();
        static Dictionary<string, string> MovieLensLinks = new Dictionary<string, string>();
        static Dictionary<int, string> TagsCodes = new Dictionary<int, string>();
        static Dictionary<int, List<int>> TagsScores = new Dictionary<int, List<int>>();

        static Dictionary<string, List<string>> ActorsData = new Dictionary<string, List<string>>(); // movieId -> actors
        static Dictionary<string, string> DirectorsData = new Dictionary<string, string>(); // movieId -> director
        static Dictionary<string, string> ActorNames = new Dictionary<string, string>(); // actorCode -> actorName


        public static void SpanReadFile(string filePath, Action<string[]> lineProcessor, char delimiter = '\t')
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine(); 
                while ((line = reader.ReadLine()) != null)
                {
                    var lineSpan = line.AsSpan();
                    var fields = new List<string>();
                    int indexPrev = 0;

                    for (int i = 0; i < lineSpan.Length; i++)
                    {
                        if (lineSpan[i] == delimiter)
                        {
                            fields.Add(lineSpan.Slice(indexPrev, i - indexPrev).ToString());
                            indexPrev = i + 1;
                        }
                    }

                    fields.Add(lineSpan.Slice(indexPrev).ToString());

                    lineProcessor(fields.ToArray());
                }
            }
        }

        public static void ReadFile(string filePath, char delimiter, Action<string[]> lineProcessor)
        {
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine();
                while (reader.ReadLine() is { } line)
                {
                    var lineData = line.Split(delimiter);
                    lineProcessor(lineData);
                }
            }
        }

        public static void LoadRatingsTagsAndActors()
        {
            SpanReadFile("../../../../Materials/Ratings_IMDB.tsv", ratingLine =>
            {
                if (ratingLine.Length >= 2 && double.TryParse(ratingLine[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double rating))
                {
                    RatingsData[ratingLine[0]] = rating;
                }
            });

            Console.WriteLine("done raitings");


            SpanReadFile("../../../../Materials/links_IMDB_MovieLens.csv", linkLine =>
            {
                if (linkLine.Length >= 2)
                {
                    MovieLensLinks[linkLine[1]] = linkLine[0];
                }
            }, ',');

            Console.WriteLine("done links");


            SpanReadFile("../../../../Materials/TagCodes_MovieLens.csv", tagLine =>
            {
                if (tagLine.Length >= 2)
                {
                    TagsCodes[int.Parse(tagLine[0])] = tagLine[1];
                }
            }, ',');

            Console.WriteLine("done tag codes");

            SpanReadFile("../../../../Materials/TagScores_MovieLens.csv", scoreLine =>
            {
                if (scoreLine.Length >= 3 && !string.IsNullOrWhiteSpace(scoreLine[0]) && !string.IsNullOrWhiteSpace(scoreLine[1]) && !string.IsNullOrWhiteSpace(scoreLine[2]))
                {
                    if (int.TryParse(scoreLine[0], out int movieLensId) && int.TryParse(scoreLine[1], out int tagId) && double.TryParse(scoreLine[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double relevance) && relevance > 0.5)
                    {
                        if (!TagsScores.ContainsKey(movieLensId))
                        {
                            TagsScores[movieLensId] = new List<int>();
                        }
                        TagsScores[movieLensId].Add(tagId);
                    }
                }
            }, ',');

            Console.WriteLine("done tag scores");


            SpanReadFile("../../../../Materials/ActorsDirectorsCodes_IMDB.tsv", actorLine =>
            {
                if (actorLine.Length >= 4)
                {
                    var movieCode = actorLine[0];
                    var actorCode = actorLine[2];
                    var role = actorLine[3];

                    if (role == "actor" || role == "actress")
                    {
                        if (!ActorsData.ContainsKey(movieCode))
                        {
                            ActorsData[movieCode] = new List<string>();
                        }
                        ActorsData[movieCode].Add(actorCode);
                    }
                    else if (role == "director")
                    {
                        DirectorsData[movieCode] = actorCode;
                    }
                }
            });

            Console.WriteLine("done actors directors codes");


            SpanReadFile("../../../../Materials/ActorsDirectorsNames_IMDB.txt", nameLine =>
            {
                if (nameLine.Length >= 2)
                {
                    var actorCode = nameLine[0];
                    var actorName = nameLine[1];
                    ActorNames[actorCode] = actorName;
                }
            });

            Console.WriteLine("done actors directors names");
        }


        public static Dictionary<string, Movie> FillInMovieDatabase(List<string[]> moviesData)
        {
            foreach (var movieLine in moviesData)
            {
                if (movieLine.Length >= 4)
                {
                    var titleId = movieLine[0];
                    var title = movieLine[2];
                    var region = movieLine[3];

                    if (region == "US" || region == "RU" || region == "GB" || region == "AU")
                    {
                        var rating = GetRating(titleId);
                        var movie = new Movie(title, rating);

                        var actors = GetActors(titleId);
                        foreach (var actor in actors)
                        {
                            movie.Actors.Add(actor);
                            if (!ActorsDirectorsDictionary.ContainsKey(actor))
                            {
                                ActorsDirectorsDictionary[actor] = new HashSet<Movie>();
                            }
                            ActorsDirectorsDictionary[actor].Add(movie);
                        }

                        var director = GetDirector(titleId);
                        if (!string.IsNullOrEmpty(director))
                        {
                            movie.Director = director;
                            if (!ActorsDirectorsDictionary.ContainsKey(director))
                            {
                                ActorsDirectorsDictionary[director] = new HashSet<Movie>();
                            }
                            ActorsDirectorsDictionary[director].Add(movie);
                        }

                        var tags = GetTags(titleId);
                        foreach (var tag in tags)
                        {
                            movie.Tags.Add(tag);
                            if (!TagsDictionary.ContainsKey(tag))
                            {
                                TagsDictionary[tag] = new HashSet<Movie>();
                            }
                            TagsDictionary[tag].Add(movie);
                        }

                        if (!MoviesDictionary.ContainsKey(title))
                        {
                            MoviesDictionary[title] = movie;
                        }
                    }
                }
            }

            return MoviesDictionary;
        }

        public static string GetDirector(string movieCode)
        {
            if (DirectorsData.TryGetValue(movieCode, out var directorCode))
            {
                if (ActorNames.TryGetValue(directorCode, out var directorName))
                {
                    return directorName;
                }
            }

            return string.Empty;
        }

        public static double GetRating(string movieCode)
        {
            return RatingsData.ContainsKey(movieCode) ? RatingsData[movieCode] : 0.0;
        }

        public static List<string> GetActors(string movieCode)
        {
            var actors = new List<string>();

            if (ActorsData.ContainsKey(movieCode))
            {
                foreach (var actorCode in ActorsData[movieCode])
                {
                    if (ActorNames.TryGetValue(actorCode, out var actorName))
                    {
                        actors.Add(actorName);
                    }
                }
            }

            return actors;
        }
        public static List<string> GetTags(string movieCode)
        {
            var tags = new List<string>();

            if (MovieLensLinks.TryGetValue(movieCode.Substring(2), out string movieLensIdStr))
            {
                if (int.TryParse(movieLensIdStr, out int movieLensId))
                {
                    if (TagsScores.TryGetValue(movieLensId, out List<int> tagIds))
                    {
                        foreach (var tagId in tagIds)
                        {
                            if (TagsCodes.TryGetValue(tagId, out string tagName))
                            {
                                tags.Add(tagName);
                            }
                        }
                    }
                }
            }

            return tags;
        }
    }
}