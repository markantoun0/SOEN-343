using System.Text.Json;
using System.Text.Json.Serialization;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services.Adapters;

/// <summary>
/// Adapts the BIXI GBFS feed into the internal MobilityLocation model.
/// </summary>
public class BixiAdapter : IBixiService, IMobilityProviderAdapter
{
    private const string StationInfoUrl = "https://gbfs.velobixi.com/gbfs/en/station_information.json";

    private readonly HttpClient _httpClient;
    private readonly ILogger<BixiAdapter> _logger;

    public BixiAdapter(HttpClient httpClient, ILogger<BixiAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public MobilityProvider Provider => MobilityProvider.Bixi;

    public Task<IEnumerable<MobilityLocation>> GetStationsAsync()
    {
        return GetLocationsAsync(new MobilityProviderRequest(0, 0));
    }

    public async Task<IEnumerable<MobilityLocation>> GetLocationsAsync(
        MobilityProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching BIXI station information from GBFS feed");

            using var response = await _httpClient.GetAsync(StationInfoUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var feed = await JsonSerializer.DeserializeAsync<GbfsFeed>(stream, cancellationToken: cancellationToken);

            if (feed?.Data?.Stations is null)
            {
                _logger.LogWarning("BIXI GBFS feed returned no stations");
                return Enumerable.Empty<MobilityLocation>();
            }

            return feed.Data.Stations.Select(MapStation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch BIXI station data");
            throw;
        }
    }

    private static MobilityLocation MapStation(GbfsStation station)
    {
        var seed = station.StationId.Aggregate(0, (acc, c) => acc + c);
        var available = new Random(seed).Next(0, station.Capacity + 1);

        return new MobilityLocation
        {
            PlaceId = station.StationId,
            Name = station.Name,
            Type = "bixi",
            City = "Montreal",
            Latitude = station.Lat,
            Longitude = station.Lon,
            Capacity = station.Capacity,
            AvailableSpots = available
        };
    }

    private record GbfsFeed([property: JsonPropertyName("data")] GbfsData? Data);

    private record GbfsData([property: JsonPropertyName("stations")] List<GbfsStation> Stations);

    private record GbfsStation(
        [property: JsonPropertyName("station_id")] string StationId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lon")] double Lon,
        [property: JsonPropertyName("capacity")] int Capacity);
}
