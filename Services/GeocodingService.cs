using System.Text.Json;

namespace SofiaTripAdvisor.Services
{
    public class GeocodingService 
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public GeocodingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;            
            _apiKey = config["GoogleMaps:ApiKey"];
        }

        public async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string location, CancellationToken ct)
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(location)}&key={_apiKey}";

            var respone = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken: ct);

            var status = respone.GetProperty("status").GetString();
            
            if(status != "OK")
            {
                throw new Exception($"Geocoding API error: {status}");
            }

            var coordinates = respone.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");

            var lat = coordinates.GetProperty("lat").GetDouble();
            var lng = coordinates.GetProperty("lng").GetDouble();

            return (lat, lng);

        }

    }
}
