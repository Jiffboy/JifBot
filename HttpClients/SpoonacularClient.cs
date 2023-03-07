using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JifBot.HttpClients
{
    public interface ISpoonacularClient
    {
        public Task<SearchResponse> GetSauceRecipe(string sauceSearchQuery);
    }

    public class SpoonacularClient : ISpoonacularClient
    {
        private readonly HttpClient _httpClient;

        public SpoonacularClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.spoonacular.com/");
        }

        public async Task<SearchResponse> GetSauceRecipe(string sauceSearchQuery = "tomato")
        {
            var queryParameters = new Dictionary<string, string>()
            {
                { "apiKey", "db3b318ab2c04661b9fa2f541cbcef43" },
                { "query", sauceSearchQuery},
                { "type", "sauce" }
            };

            var uri = "recipes/complexSearch";
            uri = QueryHelpers.AddQueryString(uri, queryParameters);

            var response = await _httpClient.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<SearchResponse>(await response.Content.ReadAsStringAsync());
            }
            else
            {
                return null;
            }
        }
    }

    public class SearchResponse
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("results")]
        public List<Recipe> Results { get; set; }

        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }
    }

    public class Recipe
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("imageType")]
        public string ImageType { get; set; }
    }
}
