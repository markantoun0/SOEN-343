using System.Text.Json;
using System.Text.Json.Serialization;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services.Adapters;

/// <summary>
/// Adapts Google Places responses into the internal MobilityLocation model.
/// </summary>
public class GooglePlacesAdapter : IMobilityService, IMobilityProviderAdapter
{
    private const string BaseUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GooglePlacesAdapter> _logger;

    public GooglePlacesAdapter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GooglePlacesAdapter> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GooglePlaces:ApiKey"]
                  ?? throw new InvalidOperationException("GooglePlaces:ApiKey is not configured.");
        _logger = logger;
    }

    public MobilityProvider Provider => MobilityProvider.GooglePlaces;

    public Task<IEnumerable<MobilityLocation>> GetNearbyMobilityLocationsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 5000)
    {
        return GetLocationsAsync(new MobilityProviderRequest(latitude, longitude, radiusMeters));
    }

    public async Task<IEnumerable<MobilityLocation>> GetLocationsAsync(
        MobilityProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var locations = new List<MobilityLocation>();

        var bikeResults = await FetchPlacesAsync(
            request.Latitude,
            request.Longitude,
            request.RadiusMeters,
            "bicycle_store",
            "bike",
            cancellationToken);
        locations.AddRange(bikeResults);

        var parkingResults = await FetchPlacesAsync(
            request.Latitude,
            request.Longitude,
            request.RadiusMeters,
            "parking",
            "parking",
            cancellationToken);
        locations.AddRange(parkingResults);

        return locations;
    }

    private async Task<IEnumerable<MobilityLocation>> FetchPlacesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        string type,
        string locationType,
        CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}?location={latitude},{longitude}&radius={radiusMeters}&type={type}&key={_apiKey}";

        try
        {
            _logger.LogInformation("Fetching {Type} places near ({Lat}, {Lng})", type, latitude, longitude);

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var placesResponse = await JsonSerializer.DeserializeAsync<PlacesApiResponse>(stream, cancellationToken: cancellationToken);

            if (placesResponse is null)
            {
                _logger.LogWarning("Empty response from Google Places API for type={Type}", type);
                return Enumerable.Empty<MobilityLocation>();
            }

            if (placesResponse.Status != "OK" && placesResponse.Status != "ZERO_RESULTS")
            {
                _logger.LogError("Google Places API error for type={Type}: {Status}", type, placesResponse.Status);
                throw new HttpRequestException($"Google Places API returned status: {placesResponse.Status}");
            }

            return placesResponse.Results.Select(result => MapPlace(result, locationType));
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

    private static MobilityLocation MapPlace(PlaceResult result, string locationType)
    {
        var seed = result.PlaceId.Aggregate(0, (acc, c) => acc + c);
        var rng = new Random(seed);
        var capacity = locationType == "parking" ? rng.Next(50, 201) : rng.Next(10, 31);
        var availableSpots = locationType == "parking" ? rng.Next(1, 29) : rng.Next(0, capacity + 1);

        return new MobilityLocation
        {
            PlaceId = result.PlaceId,
            Name = result.Name,
            Type = locationType,
            City = InferCity(result.Vicinity, result.Geometry.Location.Lat, result.Geometry.Location.Lng),
            Latitude = result.Geometry.Location.Lat,
            Longitude = result.Geometry.Location.Lng,
            Capacity = capacity,
            AvailableSpots = availableSpots
        };
    }

    private static string InferCity(string? vicinity, double latitude, double longitude)
    {
        if (!string.IsNullOrWhiteSpace(vicinity))
        {
            var text = vicinity.Trim().ToLowerInvariant();
            if (text.Contains("laval"))
                return "Laval";
            if (text.Contains("montreal") || text.Contains("montréal"))
                return "Montreal";
        }

        const double montrealLat = 45.5017;
        const double montrealLng = -73.5673;
        const double lavalLat = 45.6066;
        const double lavalLng = -73.7124;

        var montrealDistance = Math.Pow(latitude - montrealLat, 2) + Math.Pow(longitude - montrealLng, 2);
        var lavalDistance = Math.Pow(latitude - lavalLat, 2) + Math.Pow(longitude - lavalLng, 2);
        return lavalDistance < montrealDistance ? "Laval" : "Montreal";
    }

    private record PlacesApiResponse(
        [property: JsonPropertyName("results")] List<PlaceResult> Results,
        [property: JsonPropertyName("status")] string Status);

    private record PlaceResult(
        [property: JsonPropertyName("place_id")] string PlaceId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("vicinity")] string? Vicinity,
        [property: JsonPropertyName("geometry")] PlaceGeometry Geometry);

    private record PlaceGeometry([property: JsonPropertyName("location")] LatLng Location);

    private record LatLng(
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lng")] double Lng);
}
