using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MobilityController : ControllerBase
{
    private readonly IReadOnlyDictionary<MobilityProvider, IMobilityProviderAdapter> _providerAdapters;
    private readonly IMobilityLocationService _locationService;
    private readonly ILogger<MobilityController> _logger;

    private const double DefaultLat = 45.5017;
    private const double DefaultLng = -73.5673;
    private const int DefaultRadius = 8000;

    public MobilityController(
        IEnumerable<IMobilityProviderAdapter> providerAdapters,
        IMobilityLocationService locationService,
        ILogger<MobilityController> logger)
    {
        _providerAdapters = providerAdapters.ToDictionary(adapter => adapter.Provider);
        _locationService = locationService;
        _logger = logger;
    }

    [HttpGet("montreal-laval")]
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
        
        var storedTask = _locationService.GetAllAsync();

        await Task.WhenAll(bixiTask, parkingMtlTask, parkingLavalTask, storedTask);
        
        var bixi = (await bixiTask).ToList();
        var parking = (await parkingMtlTask).Where(l => l.Type == "parking").Take(50)
            .Concat((await parkingLavalTask).Where(l => l.Type == "parking").Take(50))
            .DistinctBy(l => l.PlaceId)
            .ToList();

        var apiLocations = bixi.Concat(parking).ToList();
        
        var storedByPlaceId = (await storedTask)
            .GroupBy(l => l.PlaceId)
            .ToDictionary(g => g.Key, g => g.First());
        
        foreach (var storedEntry in storedByPlaceId)
        {
            var storedLoc = storedEntry.Value;
            
            var existing = apiLocations.FirstOrDefault(l => l.PlaceId == storedLoc.PlaceId);

            if (existing != null)
            {
                existing.AvailableSpots = storedLoc.AvailableSpots;
                existing.Capacity = storedLoc.Capacity;
                existing.City = string.IsNullOrWhiteSpace(storedLoc.City) ? existing.City : storedLoc.City;
            }
            else
            {
                apiLocations.Add(storedLoc);
            }
        }

        return Ok(new
        {
            success = true,
            count = apiLocations.Count,
            locations = apiLocations
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in GetMontrealAndLaval");
        return StatusCode(500, new { success = false });
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
