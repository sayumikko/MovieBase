using Microsoft.EntityFrameworkCore;
using MovieBase;
using MovieBase.Data;
using MovieBase.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MovieBase
{
    public class BCFileParser
    {
        // Общие словари
        public static Dictionary<string, Movie> MoviesDictionary = new Dictionary<string, Movie>();
        static Dictionary<string, HashSet<Movie>> ActorsDirectorsDictionary = new Dictionary<string, HashSet<Movie>>();
        static Dictionary<string, HashSet<Movie>> TagsDictionary = new Dictionary<string, HashSet<Movie>>();

        static Dictionary<string, double> RatingsData = new Dictionary<string, double>();
        static Dictionary<string, string> MovieLensLinks = new Dictionary<string, string>();
        static Dictionary<int, string> TagsCodes = new Dictionary<int, string>();
        static Dictionary<int, List<int>> TagsScores = new Dictionary<int, List<int>>();

        static Dictionary<string, List<string>> ActorsData = new Dictionary<string, List<string>>(); // movieId -> actors
        static Dictionary<string, string> DirectorsData = new Dictionary<string, string>(); // movieId -> director
        static Dictionary<string, string> ActorNames = new Dictionary<string, string>(); // actorCode -> actorName

        // BlockingCollections для разных файлов
        private static BlockingCollection<string[]> movieCodesQueue = new BlockingCollection<string[]>();
        private static BlockingCollection<string[]> ratingsQueue = new BlockingCollection<string[]>();
        private static BlockingCollection<string[]> linksQueue = new BlockingCollection<string[]>();
        private static BlockingCollection<string[]> tagCodesQueue = new BlockingCollection<string[]>();
        private static BlockingCollection<string[]> tagScoresQueue = new BlockingCollection<string[]>();
        private static BlockingCollection<string[]> actorsDirectorsCodesQueue = new BlockingCollection<string[]>();
        private static BlockingCollection<string[]> actorNamesQueue = new BlockingCollection<string[]>();

        public static void FillInMovieDatabase()
        {
            using (var context = new ApplicationDbContext())
            {
                context.Database.EnsureCreated();

                // Подгружаем существующие записи из базы
                var existingDirectors = context.Directors.ToDictionary(d => d.Name);
                var existingActors = context.Actors.ToDictionary(a => a.Name);
                var existingTags = context.Tags.ToDictionary(t => t.Name);
                var existingMovies = context.Movies.ToDictionary(m => m.Title);
                var movieActorsDict = context.MovieActors
                    .ToLookup(ma => ma.MovieData.Title, ma => ma.Actor.Name);
                var movieTagsDict = context.MovieTags
                    .ToLookup(mt => mt.MovieData.Title, mt => mt.Tag.Name);

                var moviesToAdd = new List<MovieData>();
                var actorsToAdd = new List<Actor>();
                var tagsToAdd = new List<Tag>();
                var movieActorsToAdd = new List<MovieActor>();
                var movieTagsToAdd = new List<MovieTag>();

                Parallel.ForEach(MoviesDictionary.Values, movie =>
                {
                    Director directorEntity = null;
                    if (!string.IsNullOrEmpty(movie.Director))
                    {
                        if (!existingDirectors.TryGetValue(movie.Director, out directorEntity))
                        {
                            directorEntity = new Director { Name = movie.Director };
                            lock (existingDirectors) { existingDirectors[movie.Director] = directorEntity; }
                        }
                    }

                    var actors = new List<Actor>();
                    foreach (var actorName in movie.Actors ?? Enumerable.Empty<string>())
                    {
                        if (!existingActors.TryGetValue(actorName, out var actorEntity))
                        {
                            actorEntity = new Actor { Name = actorName };
                            lock (actorsToAdd) { actorsToAdd.Add(actorEntity); }
                            lock (existingActors) { existingActors[actorName] = actorEntity; }
                        }
                        actors.Add(actorEntity);
                    }

                    var tags = new List<Tag>();
                    foreach (var tagName in movie.Tags ?? Enumerable.Empty<string>())
                    {
                        if (!existingTags.TryGetValue(tagName, out var tagEntity))
                        {
                            tagEntity = new Tag { Name = tagName };
                            lock (tagsToAdd) { tagsToAdd.Add(tagEntity); }
                            lock (existingTags) { existingTags[tagName] = tagEntity; }
                        }
                        tags.Add(tagEntity);
                    }

                    if (!existingMovies.TryGetValue(movie.Title, out var movieEntity))
                    {
                        movieEntity = new MovieData
                        {
                            Title = movie.Title,
                            Rating = movie.Rating,
                            Director = directorEntity
                        };
                        lock (moviesToAdd) { moviesToAdd.Add(movieEntity); }
                        lock (existingMovies) { existingMovies[movie.Title] = movieEntity; }
                    }

                    foreach (var actor in actors)
                    {
                        if (!movieActorsDict[movie.Title].Contains(actor.Name))
                        {
                            var movieActor = new MovieActor { MovieData = movieEntity, Actor = actor };
                            lock (movieActorsToAdd) { movieActorsToAdd.Add(movieActor); }
                        }
                    }

                    foreach (var tag in tags)
                    {
                        if (!movieTagsDict[movie.Title].Contains(tag.Name))
                        {
                            var movieTag = new MovieTag { MovieData = movieEntity, Tag = tag };
                            lock (movieTagsToAdd) { movieTagsToAdd.Add(movieTag); }
                        }
                    }
                });

                if (moviesToAdd.Count > 0) context.Movies.AddRange(moviesToAdd);
                if (actorsToAdd.Count > 0) context.Actors.AddRange(actorsToAdd);
                if (tagsToAdd.Count > 0) context.Tags.AddRange(tagsToAdd);
                context.SaveChanges();
                Console.WriteLine($"Данные сохранены в {DateTime.Now:HH:mm:ss}");

                if (movieActorsToAdd.Count > 0) context.MovieActors.AddRange(movieActorsToAdd);
                if (movieTagsToAdd.Count > 0) context.MovieTags.AddRange(movieTagsToAdd);
                context.SaveChanges();
                Console.WriteLine($"Данные сохранены в {DateTime.Now:HH:mm:ss}");

                var movieSimilaritiesToAdd = new ConcurrentBag<MovieSimilarity>();

                Parallel.ForEach(MoviesDictionary.Values, movie =>
                {
                    var top10SimilarMovies = GetTop10SimilarMovies(movie);

                    foreach (var similarMovie in top10SimilarMovies)
                    {
                        var similarityScore = movie.CalculateSimilarity(similarMovie);

                        var movieSimilarity = new MovieSimilarity
                        {
                            MovieId = existingMovies[movie.Title].MovieDataId,
                            SimilarMovieId = existingMovies[similarMovie.Title].MovieDataId,
                            SimilarityScore = similarityScore
                        };

                        movieSimilaritiesToAdd.Add(movieSimilarity);
                    }
                });

                Console.WriteLine($"Добавляю диапазон в {DateTime.Now:HH:mm:ss}");
                context.MovieSimilarities.AddRange(movieSimilaritiesToAdd);
                context.SaveChanges();
            }
        }

        public static List<Movie> GetTop10SimilarMovies(Movie targetMovie)
        {
            var candidates = new HashSet<Movie>();

            foreach (var actor in targetMovie.Actors)
            {
                if (ActorsDirectorsDictionary.TryGetValue(actor, out var moviesWithActor))
                {
                    candidates.UnionWith(moviesWithActor);
                }
            }

            if (targetMovie.Director != null &&
                ActorsDirectorsDictionary.TryGetValue(targetMovie.Director, out var moviesWithDirector))
            {
                candidates.UnionWith(moviesWithDirector);
            }

            foreach (var tag in targetMovie.Tags)
            {
                if (TagsDictionary.TryGetValue(tag, out var moviesWithTag))
                {
                    candidates.UnionWith(moviesWithTag);
                }
            }

            // Выбираем топ-10 фильмов
            var top10SimilarMovies = candidates
                .Where(m => m != targetMovie)
                .OrderByDescending(m => targetMovie.CalculateSimilarity(m))
                .Take(10)
                .ToList();

            return top10SimilarMovies;
        }


        // Основная логика

        public static void LoadRatingsTagsAndActors()
        {
            var consumerTasks = new List<Task>
            {
                Task.Run(() => ProcessRatings()),
                Task.Run(() => ProcessLinks()),
                Task.Run(() => ProcessTagCodes()),
                Task.Run(() => ProcessTagScores()),
                Task.Run(() => ProcessActorsDirectorsCodes()),
                Task.Run(() => ProcessActorNames())
            };

            var producerTasks = new List<Task>
            {
                Task.Run(() => ReadRatingsFile("../../../../Materials/Ratings_IMDB.tsv")),
                Task.Run(() => ReadLinksFile("../../../../Materials/links_IMDB_MovieLens.csv")),
                Task.Run(() => ReadTagCodesFile("../../../../Materials/TagCodes_MovieLens.csv")),
                Task.Run(() => ReadTagScoresFile("../../../../Materials/TagScores_MovieLens.csv")),
                Task.Run(() => ReadActorsDirectorsCodesFile("../../../../Materials/ActorsDirectorsCodes_IMDB.tsv")),
                Task.Run(() => ReadActorNamesFile("../../../../Materials/ActorsDirectorsNames_IMDB.txt"))
            };

            Task.WaitAll(producerTasks.ToArray());

            ratingsQueue.CompleteAdding();
            linksQueue.CompleteAdding();
            tagCodesQueue.CompleteAdding();
            tagScoresQueue.CompleteAdding();
            actorsDirectorsCodesQueue.CompleteAdding();
            actorNamesQueue.CompleteAdding();

            Task.WaitAll(consumerTasks.ToArray());

            Console.WriteLine("Все файлы обработаны.");
        }

        // Секция чтения файлов

        private static void ReadMovieCodesFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine(); // Пропускаем заголовок
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("US") || line.Contains("RU") || line.Contains("GB") || line.Contains("AU"))
                    {
                        var fields = line.Split('\t');
                        movieCodesQueue.Add(fields);
                    }
                }
            }
        }

        private static void ReadRatingsFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split('\t');
                    ratingsQueue.Add(fields);
                }
            }
        }
        private static void ReadLinksFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(',');
                    linksQueue.Add(fields);
                }
            }
        }
        private static void ReadTagCodesFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(',');
                    tagCodesQueue.Add(fields);
                }
            }
        }
        private static void ReadActorsDirectorsCodesFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split('\t');
                    actorsDirectorsCodesQueue.Add(fields);
                }
            }
        }
        private static void ReadTagScoresFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(',');
                    tagScoresQueue.Add(fields);
                }
            }
        }
        private static void ReadActorNamesFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("actor") || line.Contains("actress") || line.Contains("director"))
                    {
                        var fields = line.Split('\t');
                        actorNamesQueue.Add(fields);
                    }
                }
            }
        }

        // Логика работы с фильмами

        public static Dictionary<string, Movie> GetMoviesDictionary()
        {
            return MoviesDictionary;
        }
        public static void FillInMovieDatabase(string[] movieLine)
        {
            if (movieLine.Length >= 4)
            {
                var titleId = movieLine[0];
                var title = movieLine[2];

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
        public static void LoadMovieCodes()
        {
            Task producerTask = Task.Run(() => ReadMovieCodesFile("../../../../Materials/MovieCodes_IMDB.tsv"));

            Task consumerTask = Task.Run(() => ProcessMovieCodes());

            producerTask.Wait();
            movieCodesQueue.CompleteAdding();

            consumerTask.Wait();
        }


        // Потребители

        // Потребитель для обработки данных из MovieCodes_IMDB.tsv
        private static void ProcessMovieCodes()
        {
            foreach (var movieLine in movieCodesQueue.GetConsumingEnumerable())
            {
                FillInMovieDatabase(movieLine);
            }
        }

        // Потребитель для файла Ratings_IMDB.tsv
        private static void ProcessRatings()
        {
            foreach (var ratingLine in ratingsQueue.GetConsumingEnumerable())
            {
                if (ratingLine.Length >= 2 && double.TryParse(ratingLine[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double rating))
                {
                    RatingsData[ratingLine[0]] = rating;
                }
            }
            Console.WriteLine("Рейтинги обработаны.");
        }

        // Потребитель для файла links_IMDB_MovieLens.csv
        private static void ProcessLinks()
        {
            foreach (var linkLine in linksQueue.GetConsumingEnumerable())
            {
                if (linkLine.Length >= 2)
                {
                    MovieLensLinks[linkLine[1]] = linkLine[0];
                }
            }
            Console.WriteLine("Ссылки обработаны.");
        }

        // Потребитель для файла TagCodes_MovieLens.csv
        private static void ProcessTagCodes()
        {
            foreach (var tagLine in tagCodesQueue.GetConsumingEnumerable())
            {
                if (tagLine.Length >= 2 && int.TryParse(tagLine[0], out int tagId))
                {
                    TagsCodes[tagId] = tagLine[1];
                }
            }
            Console.WriteLine("Коды тегов обработаны.");
        }

        // Потребитель для файла TagScores_MovieLens.csv
        private static void ProcessTagScores()
        {
            foreach (var scoreLine in tagScoresQueue.GetConsumingEnumerable())
            {
                if (scoreLine.Length >= 3 &&
                    !string.IsNullOrWhiteSpace(scoreLine[0]) &&
                    !string.IsNullOrWhiteSpace(scoreLine[1]) &&
                    !string.IsNullOrWhiteSpace(scoreLine[2]) &&
                    int.TryParse(scoreLine[0], out int movieLensId) &&
                    int.TryParse(scoreLine[1], out int tagId) &&
                    double.TryParse(scoreLine[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double relevance) &&
                    relevance > 0.5)
                {
                    if (!TagsScores.ContainsKey(movieLensId))
                    {
                        TagsScores[movieLensId] = new List<int>();
                    }
                    TagsScores[movieLensId].Add(tagId);
                }
            }
            Console.WriteLine("Оценки тегов обработаны.");
        }

        // Потребитель для файла ActorsDirectorsCodes_IMDB.tsv
        private static void ProcessActorsDirectorsCodes()
        {
            foreach (var actorLine in actorsDirectorsCodesQueue.GetConsumingEnumerable())
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
            }
            Console.WriteLine("Коды актёров и режиссёров обработаны.");
        }

        // Потребитель для файла ActorsDirectorsNames_IMDB.txt
        private static void ProcessActorNames()
        {
            foreach (var nameLine in actorNamesQueue.GetConsumingEnumerable())
            {
                if (nameLine.Length >= 2)
                {
                    var actorCode = nameLine[0];
                    var actorName = nameLine[1];
                    ActorNames[actorCode] = actorName;
                }
            }
            Console.WriteLine("Имена актёров и режиссёров обработаны.");
        }


        // Заполнение данными
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