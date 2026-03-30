using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MobilityController : ControllerBase
{
    private readonly IReadOnlyDictionary<MobilityProvider, IMobilityProviderAdapter> _providerAdapters;
    private readonly IMobilityLocationService _locationService;
    private readonly IRouteService _routeService;
    private readonly ILogger<MobilityController> _logger;

    private const double DefaultLat = 45.5017;
    private const double DefaultLng = -73.5673;
    private const int DefaultRadius = 8000;

    private static readonly HashSet<string> SupportedTravelModes =
        new(StringComparer.OrdinalIgnoreCase) { "car", "bike" };

    public MobilityController(
        IEnumerable<IMobilityProviderAdapter> providerAdapters,
        IMobilityLocationService locationService,
        IRouteService routeService,
        ILogger<MobilityController> logger)
    {
        _providerAdapters = providerAdapters.ToDictionary(adapter => adapter.Provider);
        _locationService = locationService;
        _routeService = routeService;
        _logger = logger;
    }

    [HttpGet("montreal-laval")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMontrealAndLaval()
    {
        try
        {
            var bixiTask = GetAdapter(MobilityProvider.Bixi)
                .GetLocationsAsync(new MobilityProviderRequest(DefaultLat, DefaultLng));
            var parkingMtlTask = GetAdapter(MobilityProvider.GooglePlaces)
                .GetLocationsAsync(new MobilityProviderRequest(45.5017, -73.5673, 10000));
            var parkingLavalTask = GetAdapter(MobilityProvider.GooglePlaces)
                .GetLocationsAsync(new MobilityProviderRequest(45.6066, -73.7124, 8000));

            await Task.WhenAll(bixiTask, parkingMtlTask, parkingLavalTask);

            var bixi = (await bixiTask).ToList();
            var parking = (await parkingMtlTask)
                .Where(l => l.Type == "parking").Take(15)
                .Concat((await parkingLavalTask)
                    .Where(l => l.Type == "parking").Take(15))
                .DistinctBy(l => l.PlaceId)
                .ToList();

            var all = bixi.Concat(parking).ToList();

            var storedByPlaceId = (await _locationService.GetAllAsync())
                .GroupBy(l => l.PlaceId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var location in all)
            {
                if (!storedByPlaceId.TryGetValue(location.PlaceId, out var stored))
                    continue;

                location.AvailableSpots = stored.AvailableSpots;
                location.Capacity = stored.Capacity;
                location.City = string.IsNullOrWhiteSpace(stored.City) ? location.City : stored.City;
            }

            return Ok(new
            {
                success = true,
                count = all.Count,
                bixiCount = bixi.Count,
                parkingCount = parking.Count,
                locations = all
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API call failed");
            return StatusCode(502, new { success = false, message = "Failed to reach an external API.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in MobilityController");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpGet("nearby")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat = DefaultLat,
        [FromQuery] double lng = DefaultLng,
        [FromQuery] int radius = DefaultRadius)
    {
        try
        {
            var locations = (await GetAdapter(MobilityProvider.GooglePlaces)
                    .GetLocationsAsync(new MobilityProviderRequest(lat, lng, radius)))
                .ToList();

            return Ok(new { success = true, count = locations.Count, locations });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Google Places API call failed");
            return StatusCode(502, new { success = false, message = "Failed to reach Google Places API.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    [HttpGet("stored")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoredLocations([FromQuery] string? type = null)
    {
        var locations = (await _locationService.GetAllAsync(type)).ToList();
        return Ok(new { success = true, count = locations.Count, locations });
    }

    [HttpGet("stored/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStoredLocationById(int id)
    {
        var location = await _locationService.GetByIdAsync(id);
        if (location is null)
            return NotFound(new { success = false, message = $"No location found with Id={id}." });

        return Ok(new { success = true, location });
    }

    [HttpPost("stored")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { success = false, message = "Name and Type are required." });

        var location = await _locationService.InsertAsync(
            placeId: request.PlaceId,
            name: request.Name,
            type: request.Type,
            city: request.City,
            latitude: request.Latitude,
            longitude: request.Longitude,
            capacity: request.Capacity,
            availableSpots: request.AvailableSpots);

        return CreatedAtAction(
            nameof(GetStoredLocationById),
            new { id = location.Id },
            new { success = true, location });
    }

    [HttpPatch("stored/{id:int}/spots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpots(int id, [FromBody] UpdateSpotsRequest request)
    {
        var location = await _locationService.UpdateAvailableSpotsAsync(id, request.AvailableSpots);
        if (location is null)
            return NotFound(new { success = false, message = $"No location found with Id={id}." });

        return Ok(new { success = true, location });
    }

    [HttpDelete("stored/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var deleted = await _locationService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { success = false, message = $"No location found with Id={id}." });

        return Ok(new { success = true, message = $"Location Id={id} deleted." });
    }

    [HttpPost("route")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ComputeRoute([FromBody] RouteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Origin))
            return BadRequest(new { success = false, message = "Origin is required." });

        if (string.IsNullOrWhiteSpace(request.Destination))
            return BadRequest(new { success = false, message = "Destination is required." });

        if (!SupportedTravelModes.Contains(request.TravelMode ?? string.Empty))
            return BadRequest(new { success = false, message = "TravelMode must be 'car' or 'bike'." });

        try
        {
            var result = await _routeService.ComputeRouteAsync(
                request.Origin.Trim(),
                request.Destination.Trim(),
                request.TravelMode!.Trim());

            return Ok(new
            {
                success = true,
                distanceMeters = result.DistanceMeters,
                duration = result.Duration,
                encodedPolyline = result.EncodedPolyline
            });
        }
        catch (GoogleRoutesException ex)
        {
            _logger.LogError(ex, "Google Routes API call failed");
            return StatusCode(502, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error computing route");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
        }
    }

    private IMobilityProviderAdapter GetAdapter(MobilityProvider provider)
    {
        if (_providerAdapters.TryGetValue(provider, out var adapter))
            return adapter;

        throw new InvalidOperationException($"No adapter is registered for mobility provider {provider}.");
    }
}

public class CreateLocationRequest
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Capacity { get; set; }
    public int AvailableSpots { get; set; }
}

public class UpdateSpotsRequest
{
    public int AvailableSpots { get; set; }
}

public class RouteRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string TravelMode { get; set; } = string.Empty;
}
