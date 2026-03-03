using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MobilityController : ControllerBase
{
    private readonly IBixiService _bixiService;
    private readonly IMobilityService _mobilityService;
    private readonly IMobilityLocationService _locationService;
    private readonly ILogger<MobilityController> _logger;

    private const double DefaultLat    = 45.5017;
    private const double DefaultLng    = -73.5673;
    private const int    DefaultRadius = 8000;

    public MobilityController(
        IBixiService bixiService,
        IMobilityService mobilityService,
        IMobilityLocationService locationService,
        ILogger<MobilityController> logger)
    {
        _bixiService     = bixiService;
        _mobilityService = mobilityService;
        _locationService = locationService;
        _logger          = logger;
    }

    // ── Live external feeds ───────────────────────────────────────────────────

    /// <summary>Returns all BIXI stations + nearby parking for Montréal and Laval.</summary>
    [HttpGet("montreal-laval")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMontrealAndLaval()
    {
        try
        {
            var bixiTask         = _bixiService.GetStationsAsync();
            var parkingMtlTask   = _mobilityService.GetNearbyMobilityLocationsAsync(45.5017, -73.5673, 10000);
            var parkingLavalTask = _mobilityService.GetNearbyMobilityLocationsAsync(45.6066, -73.7124, 8000);

            await Task.WhenAll(bixiTask, parkingMtlTask, parkingLavalTask);

            var bixi    = (await bixiTask).ToList();
            var parking = (await parkingMtlTask)
                            .Where(l => l.Type == "parking").Take(15)
                            .Concat((await parkingLavalTask)
                                .Where(l => l.Type == "parking").Take(15))
                            .DistinctBy(l => l.PlaceId)
                            .ToList();

            var all = bixi.Concat(parking).ToList();
            return Ok(new { success = true, count = all.Count, bixiCount = bixi.Count, parkingCount = parking.Count, locations = all });
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

    /// <summary>Returns parking locations near custom coordinates (Google Places).</summary>
    [HttpGet("nearby")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat    = DefaultLat,
        [FromQuery] double lng    = DefaultLng,
        [FromQuery] int    radius = DefaultRadius)
    {
        try
        {
            var locations = (await _mobilityService.GetNearbyMobilityLocationsAsync(lat, lng, radius)).ToList();
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

    // ── Database CRUD (via MobilityLocationService) ───────────────────────────

    /// <summary>Retrieve all stored locations, optionally filtered by ?type=bike|parking</summary>
    [HttpGet("stored")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoredLocations([FromQuery] string? type = null)
    {
        var locations = (await _locationService.GetAllAsync(type)).ToList();
        return Ok(new { success = true, count = locations.Count, locations });
    }

    /// <summary>Retrieve a single stored location by its database ID</summary>
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

    /// <summary>Insert a new location with all values into the database</summary>
    [HttpPost("stored")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { success = false, message = "Name and Type are required." });

        var location = await _locationService.InsertAsync(
            placeId:        request.PlaceId,
            name:           request.Name,
            type:           request.Type,
            city:           request.City,
            latitude:       request.Latitude,
            longitude:      request.Longitude,
            capacity:       request.Capacity,
            availableSpots: request.AvailableSpots);

        return CreatedAtAction(
            nameof(GetStoredLocationById),
            new { id = location.Id },
            new { success = true, location });
    }

    /// <summary>Update available spots for a stored location</summary>
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

    /// <summary>Delete a stored location by ID</summary>
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
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class CreateLocationRequest
{
    public string PlaceId        { get; set; } = string.Empty;
    public string Name           { get; set; } = string.Empty;
    public string Type           { get; set; } = string.Empty;
    public string City           { get; set; } = string.Empty;
    public double Latitude       { get; set; }
    public double Longitude      { get; set; }
    public int    Capacity       { get; set; }
    public int    AvailableSpots { get; set; }
}

public class UpdateSpotsRequest
{
    public int AvailableSpots { get; set; }
}
