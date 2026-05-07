using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace SofiaTripAdvisor.Services
{
    public class GetNearbyPlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public GetNearbyPlacesService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GoogleMaps:ApiKey"];
        }

        public async Task<List<PlaceResult>> GetNearbyPlacesAsync(double lat, double lng, string placeType, string keywords, CancellationToken ct)
        {

            var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json" +
              $"?location={lat},{lng}" +
              $"&radius=1000" +
              $"&type={placeType}" +
              $"&keyword={Uri.EscapeDataString(keywords)}" +
              $"&key={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken: ct);

            var places = response.GetProperty("results")
             .EnumerateArray()
             .Select(p => new PlaceResult(
                p.GetProperty("name").GetString()!,
                p.TryGetProperty("rating", out var r) ? r.GetDouble() : 0,
                p.GetProperty("vicinity").GetString()!,
                p.TryGetProperty("user_ratings_total", out var u) ? u.GetInt32() : 0,
                p.TryGetProperty("opening_hours", out var o) && o.TryGetProperty("open_now", out var on) && on.GetBoolean(),
                p.TryGetProperty("price_level", out var p1) ? p1.GetInt32() : -1
))
             .ToList();

            return places;

        }

    }
    public record PlaceResult(
    string Name,
    double Rating,
    string Address,
    int UserRatingsTotal,
    bool OpenNow,
    int PriceLevel     // 0–4, -1 if not available
    );
}
