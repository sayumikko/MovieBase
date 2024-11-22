using MovieBase;
using MovieBase.Data;
using System;
using System.Collections.Generic;

class Program
{
    public static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("\nВыберите режим работы:");
            Console.WriteLine("1 - Поиск фильма в базе данных");
            Console.WriteLine("2 - Парсинг файлов с помощью FileParser");
            Console.WriteLine("3 - Парсинг файлов с помощью BCFileParser");
            Console.WriteLine("4 - Заполнение базы данных");
            Console.WriteLine("5 - Найти топ-10 похожих фильмов (без использования базы данных)");
            Console.WriteLine("0 - Выход");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    RunSearchMovie();
                    break;

                case "2":
                    RunFileParsingWithFileParser();
                    break;

                case "3":
                    RunFileParsingWithBCFileParser();
                    break;

                case "4":
                    RunDatabaseFilling();
                    break;

                case "5":
                    DisplayTop10SimilarMovies();
                    break;

                case "0":
                    Console.WriteLine("Выход из программы...");
                    return;

                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    break;
            }
        }
    }

    // 1. Поиск фильма в базе данных
    private static void RunSearchMovie()
    {
        Input.SearchAndDisplayMovie(new ApplicationDbContext());
    }

    // 2. Парсинг файлов с помощью FileParser и загрузка данных в базу данных
    private static void RunFileParsingWithFileParser()
    {
        var moviesData = new List<string[]>();
        FileParser.SpanReadFile("../../../../Materials/MovieCodes_IMDB.tsv", line =>
        {
            moviesData.Add(line);
        }, '\t');

        FileParser.LoadRatingsTagsAndActors();
        var moviesDictionary = FileParser.FillInMovieDatabase(moviesData);
        Input.SearchAndDisplayMovie(moviesDictionary);
    }

    // 3. Парсинг файлов с помощью BCFileParser и загрузка данных в базу данных
    private static void RunFileParsingWithBCFileParser()
    {
        BCFileParser.LoadRatingsTagsAndActors();
        BCFileParser.LoadMovieCodes();
        var moviesDictionary = BCFileParser.GetMoviesDictionary();
        Input.SearchAndDisplayMovie(moviesDictionary);
    }

    // 4. Парсинг файлов, загрузка данных и заполнение базы данных
    private static void RunDatabaseFilling()
    {
        Console.WriteLine("Начинаем парсинг файлов...");
        BCFileParser.LoadRatingsTagsAndActors();
        BCFileParser.LoadMovieCodes();

        Console.WriteLine("Заполняем базу данных...");
        BCFileParser.FillInMovieDatabase();

        Console.WriteLine("Готово. Теперь можно искать фильмы.");
        RunSearchMovie();
    }

    // 5. Получение топ-10 похожих фильмов без базы данных
    private static void DisplayTop10SimilarMovies()
    {
        BCFileParser.LoadRatingsTagsAndActors();
        BCFileParser.LoadMovieCodes();

        while (true)
        {

            Console.WriteLine("Введите название фильма:");
            var movieTitle = Console.ReadLine();

            if (BCFileParser.MoviesDictionary.TryGetValue(movieTitle, out var targetMovie))
            {
                var similarMovies = BCFileParser.GetTop10SimilarMovies(targetMovie);
                Console.WriteLine($"Топ-10 похожих фильмов на '{movieTitle}':");

                foreach (var movie in similarMovies)
                {
                    Console.WriteLine($"{movie.Title} — похожесть: {targetMovie.CalculateSimilarity(movie):0.00}");
                }
            }
            else
            {
                Console.WriteLine($"Фильм '{movieTitle}' не найден.");
            }
        }
    }
}
