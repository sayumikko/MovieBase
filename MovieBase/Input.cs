using Microsoft.EntityFrameworkCore;
using MovieBase.Data;
using System;
using System.Collections.Generic;

namespace MovieBase
{
    public static class Input
    {
        public static void SearchAndDisplayMovie(ApplicationDbContext dbContext)
        {
            while (true)
            {
                Console.WriteLine("Введите название фильма:");
                string movieTitle = Console.ReadLine();

                var movie = dbContext.Movies
                                     .Include(m => m.Director)
                                     .Include(m => m.MovieActors)
                                        .ThenInclude(ma => ma.Actor)
                                     .Include(m => m.MovieTags)
                                        .ThenInclude(mt => mt.Tag)
                                     .FirstOrDefault(m => m.Title == movieTitle);
                

                if (movie != null)
                {
                    Output.PrintMovieInfo(movie);
                }
                else
                {
                    Console.WriteLine("Фильм не найден.");
                }
            }
        }


        public static void SearchAndDisplayMovie(Dictionary<string, Movie> moviesDictionary)
        {
            while (true)
            {
                Console.WriteLine("Введите название фильма:");
                string movieTitle = Console.ReadLine();

                if (moviesDictionary.ContainsKey(movieTitle))
                {
                    Output.PrintMovieInfo(moviesDictionary[movieTitle]);
                }
                else
                {
                    Console.WriteLine("Фильм не найден.");
                }
            }
        }
    }
}
