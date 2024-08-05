using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NBAGame
{
    public interface INBAApiClient
    {
        Task<string> GetPlayByPlayDataAsync(string gameId);
    }

    /**
     * Get the data from the NBA API
     **/
    public class NBAApiClient(HttpClient httpClient) : INBAApiClient
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public async Task<string> GetPlayByPlayDataAsync(string gameId)
        {
            string apiUrl = $"https://cdn.nba.com/static/json/liveData/playbyplay/playbyplay_{gameId}.json";
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception($"Failed to fetch data from NBA API: {response.StatusCode}");
            }
        }
    }
}
