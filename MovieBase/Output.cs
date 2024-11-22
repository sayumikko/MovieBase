using MovieBase.Models;
using System;

namespace MovieBase
{
    public static class Output
    {
        public static void PrintMovieInfo(MovieData movie)
        {
            Console.WriteLine($"Название: {movie.Title}");
            Console.WriteLine($"Режиссёр: {movie.Director?.Name}");
            Console.WriteLine($"Рейтинг: {movie.Rating}");

            Console.WriteLine("Актёры:");
            foreach (var actor in movie.MovieActors.Select(ma => ma.Actor.Name))
            {
                Console.WriteLine($"- {actor}");
            }

            Console.WriteLine("Теги:");
            foreach (var tag in movie.MovieTags.Select(mt => mt.Tag.Name))
            {
                Console.WriteLine($"- {tag}");
            }
        }


        public static void PrintMovieInfo(Movie movie)
        {
            Console.WriteLine($"Название: {movie.Title}");
            Console.WriteLine($"Рейтинг: {movie.Rating}");
            Console.WriteLine($"Режиссер: {movie.Director}");
            Console.WriteLine("Актеры:");
            foreach (var actor in movie.Actors)
            {
                Console.WriteLine($" - {actor}");
            }
            Console.WriteLine("Теги:");
            foreach (var tag in movie.Tags)
            {
                Console.WriteLine($" - {tag}");
            }
        }
    }
}
