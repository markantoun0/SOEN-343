using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class GoogleRoutesService : IRouteService
{
    private const string ComputeRoutesUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";
    private const string FieldMask = "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline";

    private static readonly Dictionary<string, string> TravelModeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["car"] = "DRIVE",
        ["bike"] = "BICYCLE"
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GoogleRoutesService> _logger;

    public GoogleRoutesService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleRoutesService> logger)
    {
        _httpClient = httpClient;
        var key = configuration["GoogleRoutes:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "GoogleRoutes:ApiKey is not configured. Set GOOGLE_ROUTES_API_KEY in your .env file.");
        _apiKey = key;
        _logger = logger;
    }

    public async Task<RouteResult> ComputeRouteAsync(
        string origin,
        string destination,
        string travelMode,
        CancellationToken cancellationToken = default)
    {
        if (!TravelModeMap.TryGetValue(travelMode, out var googleTravelMode))
            throw new ArgumentException($"Unsupported travel mode: {travelMode}");

        var requestBody = new
        {
            origin = new { address = origin },
            destination = new { address = destination },
            travelMode = googleTravelMode
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, ComputeRoutesUrl);
        request.Content = content;
        request.Headers.Add("X-Goog-Api-Key", _apiKey);
        request.Headers.Add("X-Goog-FieldMask", FieldMask);

        _logger.LogInformation(
            "Requesting route from {Origin} to {Destination} via {Mode}",
            origin, destination, googleTravelMode);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Google Routes API");
            throw new GoogleRoutesException("Failed to reach Google Routes API.", inner: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Google Routes API returned {StatusCode}: {Body}",
                (int)response.StatusCode, errorBody);
            throw new GoogleRoutesException(
                $"Google Routes API returned status {(int)response.StatusCode}: {errorBody}",
                (int)response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var routesResponse = await JsonSerializer.DeserializeAsync<RoutesApiResponse>(stream, cancellationToken: cancellationToken);

        if (routesResponse?.Routes is not { Count: > 0 })
        {
            _logger.LogWarning("No routes returned for {Origin} -> {Destination}", origin, destination);
            throw new GoogleRoutesException("No route found for the given origin and destination.");
        }

        var route = routesResponse.Routes[0];
        return new RouteResult(
            route.DistanceMeters,
            route.Duration ?? "0s",
            route.Polyline?.EncodedPolyline ?? string.Empty);
    }

    private sealed record RoutesApiResponse(
        [property: JsonPropertyName("routes")] List<RouteEntry>? Routes);

    private sealed record RouteEntry(
        [property: JsonPropertyName("distanceMeters")] int DistanceMeters,
        [property: JsonPropertyName("duration")] string? Duration,
        [property: JsonPropertyName("polyline")] PolylineEntry? Polyline);

    private sealed record PolylineEntry(
        [property: JsonPropertyName("encodedPolyline")] string? EncodedPolyline);
}
