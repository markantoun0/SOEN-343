using System.Text.Json;
using System.Text.Json.Serialization;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

/// <summary>
/// Fetches real-time BIXI bike-share station data from the public GBFS feed.
/// No API key required.
/// </summary>
public class BixiService : IBixiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BixiService> _logger;

    private const string StationInfoUrl =
        "https://gbfs.velobixi.com/gbfs/en/station_information.json";

    public BixiService(HttpClient httpClient, ILogger<BixiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<MobilityLocation>> GetStationsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching BIXI station information from GBFS feed...");

            var json = await _httpClient.GetStringAsync(StationInfoUrl);
            var feed = JsonSerializer.Deserialize<GbfsFeed>(json);

            if (feed?.Data?.Stations == null)
            {
                _logger.LogWarning("BIXI GBFS feed returned no stations.");
                return Enumerable.Empty<MobilityLocation>();
            }

            _logger.LogInformation("BIXI feed returned {Count} stations.", feed.Data.Stations.Count);

            return feed.Data.Stations.Select(s => new MobilityLocation
            {
                PlaceId   = s.StationId,
                Name      = s.Name,
                Type      = "bixi",
                Latitude  = s.Lat,
                Longitude = s.Lon,
                Vicinity  = $"Capacity: {s.Capacity} docks"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch BIXI station data.");
            throw;
        }
    }

    // ── GBFS response DTOs ────────────────────────────────────────────────────

    private record GbfsFeed(
        [property: JsonPropertyName("data")] GbfsData? Data
    );

    private record GbfsData(
        [property: JsonPropertyName("stations")] List<GbfsStation> Stations
    );

    private record GbfsStation(
        [property: JsonPropertyName("station_id")] string StationId,
        [property: JsonPropertyName("name")]       string Name,
        [property: JsonPropertyName("lat")]        double Lat,
        [property: JsonPropertyName("lon")]        double Lon,
        [property: JsonPropertyName("capacity")]   int    Capacity
    );
}

