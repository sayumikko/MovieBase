using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieBase;
using MovieBase.Data;


namespace MovieBaseBlazorApp.Components.Services
{
    public class MovieService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public MovieService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Movie> GetRandomMovieAsync()
        {
            var random = new Random();
            int totalMovies = await GetTotalMoviesCountAsync();
            int skip = random.Next(0, totalMovies);

            var movieData = await _context.Movies
                .Include(m => m.Director)
                .Skip(skip)
                .Take(1)
                .FirstOrDefaultAsync();

            if (movieData == null)
            {
                throw new Exception("No movies found in the database.");
            }

            return new Movie(movieData.Title, movieData.Rating)
            {
                Director = movieData.Director?.Name,
                Actors = new HashSet<string>() 
            };
        }

        public async Task<List<string>> GetTagsForMovieAsync(string title)
        {
            return await _context.Movies
                .Where((m => m.Title == title))
                .SelectMany(m => m.MovieTags.Select(mt => mt.Tag.Name))
                .ToListAsync();
        }


        public async Task<int> GetTotalMoviesCountAsync()
        {
            return await _context.Movies.CountAsync();
        }


        public async Task<List<Movie>> GetTopSimilarMoviesAsync(string movieTitle)
        {
            var movieData = await _context.Movies
                .Include(m => m.SimilarMovies)
                .FirstOrDefaultAsync(m => m.Title == movieTitle);

            if (movieData == null) return new List<Movie>();

            var similarMovies = await _context.MovieSimilarities
                .Where(ms => ms.MovieId == movieData.MovieDataId) 
                .Select(ms => ms.SimilarMovie)
                .Where(sm => sm.Title != movieTitle) 
                .ToListAsync();

            var result = similarMovies.Select(sm => new Movie(sm.Title, sm.Rating)).ToList();

            return result;
        }

        public async Task<List<Movie>> SearchExactMoviesAsync(string searchTerm, int page, int pageSize)
        {
            string cacheKey = $"Search_{searchTerm}_{page}_{pageSize}";
            if (!_cache.TryGetValue(cacheKey, out List<Movie> movies))
            {
                movies = await _context.Movies
                    .Where(m => m.Title == searchTerm)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new Movie(m.Title, m.Rating)
                    {
                        Director = m.Director.Name,
                    })
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                _cache.Set(cacheKey, movies, cacheOptions);
            }
            return movies;
        }


        public async Task<List<Movie>> SearchMoviesAsync(string searchTerm, int page, int pageSize)
        {
            string cacheKey = $"Search_{searchTerm}_{page}_{pageSize}";
            if (!_cache.TryGetValue(cacheKey, out List<Movie> movies))
            {
                movies = await _context.Movies
                    .Where(m => m.Title.Contains(searchTerm))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new Movie(m.Title, m.Rating)
                    {
                        Director = m.Director.Name,
                    })
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                _cache.Set(cacheKey, movies, cacheOptions);
            }
            return movies;
        }

        public async Task<int> GetSearchResultCountAsync(string searchTerm)
        {
            return await _context.Movies
                .CountAsync(m => m.Title.Contains(searchTerm));
        }
        public async Task<List<string>> GetActorsForMovieAsync(string titleId)
        {
            string cacheKey = $"MovieActors_{titleId}";
            if (!_cache.TryGetValue(cacheKey, out List<string> actors))
            {
                actors = await _context.Movies
                    .Where(m => m.Title == titleId)
                    .SelectMany(m => m.MovieActors.Select(ma => ma.Actor.Name))
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                _cache.Set(cacheKey, actors, cacheOptions);
            }
            return actors;
        }

        public async Task<List<Movie>> SearchMoviesByTagAsync(string tag, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(tag)) return new List<Movie>();

            var movies = await _context.MovieTags
                .Where(mt => mt.Tag.Name.Contains(tag)) 
                .Select(mt => mt.MovieData)             
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Title,
                    m.Rating,
                    DirectorName = m.Director.Name
                })
                .ToListAsync();

            return movies.Select(m => new Movie(m.Title, m.Rating)
            {
                Director = m.DirectorName,
                Actors = new HashSet<string>()
            }).ToList();
        }

        public async Task<int> GetMoviesByTagCountAsync(string tagName)
        {
            return await _context.Movies
                .CountAsync(m => m.MovieTags.Any(mt => mt.Tag.Name == tagName));
        }


        public async Task<List<Movie>> GetMoviesAsync(int page, int pageSize)
        {
            string cacheKey = $"MoviePage_{page}_{pageSize}";
            if (!_cache.TryGetValue(cacheKey, out List<Movie> movies))
            {
                var movieDataList = await _context.Movies
                    .Include(m => m.Director)
                    .OrderBy(m => m.Title)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                movies = movieDataList.Select(m => new Movie(m.Title, m.Rating)
                {
                    Director = m.Director?.Name,
                    Actors = m.MovieActors.Select(ma => ma.Actor.Name).Where(name => name != null).ToHashSet()
                }).ToList();


                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                _cache.Set(cacheKey, movies, cacheOptions);
            }
            return movies;
        }

        public async Task<List<Movie>> SearchMoviesByActorAsync(string actorName, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(actorName)) return new List<Movie>();

            var movies = await _context.MovieActors
                .Where(ma => ma.Actor.Name.StartsWith(actorName))
                .Select(ma => ma.MovieData)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Title,
                    m.Rating,
                    DirectorName = m.Director.Name
                })
                .ToListAsync();

            return movies.Select(m => new Movie(m.Title, m.Rating)
            {
                Director = m.DirectorName,
                Actors = new HashSet<string>()
            }).ToList();
        }

        public async Task<int> GetMoviesByActorCountAsync(string actorName)
        {
            return await _context.MovieActors
                .CountAsync(ma => ma.Actor.Name.Contains(actorName));
        }


    }
}
