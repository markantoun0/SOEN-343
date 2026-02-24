using System.Text.Json;
using System.Text.Json.Serialization;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class GooglePlacesService : IMobilityService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GooglePlacesService> _logger;

    private const string BaseUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json";

    public GooglePlacesService(HttpClient httpClient, IConfiguration configuration, ILogger<GooglePlacesService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GooglePlaces:ApiKey"]
                  ?? throw new InvalidOperationException("GooglePlaces:ApiKey is not configured.");
        _logger = logger;
    }

    public async Task<IEnumerable<MobilityLocation>> GetNearbyMobilityLocationsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 5000)
    {
        var locations = new List<MobilityLocation>();

        // Fetch bike-related locations
        var bikeResults = await FetchPlacesAsync(latitude, longitude, radiusMeters, "bicycle_store", "bike");
        locations.AddRange(bikeResults);

        // Fetch parking locations
        var parkingResults = await FetchPlacesAsync(latitude, longitude, radiusMeters, "parking", "parking");
        locations.AddRange(parkingResults);

        return locations;
    }

    private async Task<IEnumerable<MobilityLocation>> FetchPlacesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        string type,
        string locationType)
    {
        var url = $"{BaseUrl}?location={latitude},{longitude}&radius={radiusMeters}&type={type}&key={_apiKey}";

        try
        {
            _logger.LogInformation("Fetching {Type} places near ({Lat}, {Lng})", type, latitude, longitude);

            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<PlacesApiResponse>(json);

            if (response == null)
            {
                _logger.LogWarning("Empty response from Google Places API for type={Type}", type);
                return Enumerable.Empty<MobilityLocation>();
            }

            if (response.Status != "OK" && response.Status != "ZERO_RESULTS")
            {
                _logger.LogError("Google Places API error for type={Type}: {Status}", type, response.Status);
                throw new HttpRequestException($"Google Places API returned status: {response.Status}");
            }

            return response.Results.Select(r =>
            {
                int? availableSpots = null;
                if (locationType == "parking")
                {
                    // Deterministic seed from PlaceId so the same spot always gets the same number
                    var seed = r.PlaceId.Aggregate(0, (acc, c) => acc + c);
                    availableSpots = new Random(seed).Next(1, 29); // 1–28 inclusive
                }
                return new MobilityLocation
                {
                    PlaceId        = r.PlaceId,
                    Name           = r.Name,
                    Type           = locationType,
                    Latitude       = r.Geometry.Location.Lat,
                    Longitude      = r.Geometry.Location.Lng,
                    Vicinity       = r.Vicinity,
                    AvailableSpots = availableSpots
                };
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching {Type} places", type);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching {Type} places", type);
            throw;
        }
    }

    // ─── Internal DTOs for deserialising Google Places response ───────────────

    private record PlacesApiResponse(
        [property: JsonPropertyName("results")] List<PlaceResult> Results,
        [property: JsonPropertyName("status")]  string Status
    );

    private record PlaceResult(
        [property: JsonPropertyName("place_id")]  string PlaceId,
        [property: JsonPropertyName("name")]      string Name,
        [property: JsonPropertyName("vicinity")]  string? Vicinity,
        [property: JsonPropertyName("geometry")]  PlaceGeometry Geometry
    );

    private record PlaceGeometry(
        [property: JsonPropertyName("location")] LatLng Location
    );

    private record LatLng(
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lng")] double Lng
    );
}




