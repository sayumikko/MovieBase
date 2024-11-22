namespace MovieBaseBlazorApp.Components.Services
{
    public class ImdbApi
    {
        private readonly HttpClient _httpClient;

        public ImdbApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OmdbResponse> GetMovieDetailsAsync(string movieTitle)
        {
            var apiKey = "1ae6316a";
            var url = $"https://www.omdbapi.com/?apikey={apiKey}&t={Uri.EscapeDataString(movieTitle)}";

            var response = await _httpClient.GetFromJsonAsync<OmdbResponse>(url);

            return response;
        }

        public class OmdbResponse
        {
            public string imdbID { get; set; }
            public string Title { get; set; }
            public string Poster { get; set; }
            public string Plot { get; set; } 
        }
    }
}
