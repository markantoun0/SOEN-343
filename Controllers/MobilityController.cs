using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MobilityController : ControllerBase
{
    private readonly IBixiService _bixiService;
    private readonly IMobilityService _mobilityService; // parking only
    private readonly ILogger<MobilityController> _logger;

    // Default Montréal city centre coordinates
    private const double DefaultLat = 45.5017;
    private const double DefaultLng = -73.5673;
    private const int    DefaultRadius = 8000; // 8 km

    public MobilityController(
        IBixiService bixiService,
        IMobilityService mobilityService,
        ILogger<MobilityController> logger)
    {
        _bixiService     = bixiService;
        _mobilityService = mobilityService;
        _logger          = logger;
    }

    /// <summary>
    /// Returns all BIXI stations + nearby parking for Montréal and Laval.
    /// </summary>
    [HttpGet("montreal-laval")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMontrealAndLaval()
    {
        try
        {
            // BIXI stations — real-time from public GBFS feed, no key needed
            var bixiTask = _bixiService.GetStationsAsync();

            // Parking — from Google Places (Montréal + Laval, capped at 15 each)
            var parkingMtlTask  = _mobilityService.GetNearbyMobilityLocationsAsync(45.5017, -73.5673, 10000);
            var parkingLavalTask = _mobilityService.GetNearbyMobilityLocationsAsync(45.6066, -73.7124, 8000);

            await Task.WhenAll(bixiTask, parkingMtlTask, parkingLavalTask);

            var bixi    = (await bixiTask).ToList();
            var parking = (await parkingMtlTask)
                            .Where(l => l.Type == "parking")
                            .Take(15)
                            .Concat((await parkingLavalTask)
                                .Where(l => l.Type == "parking")
                                .Take(15))
                            .DistinctBy(l => l.PlaceId)
                            .ToList();

            var all = bixi.Concat(parking).ToList();

            return Ok(new { success = true, count = all.Count, bixiCount = bixi.Count, parkingCount = parking.Count, locations = all });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API call failed");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { success = false, message = "Failed to reach an external API.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in MobilityController");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Returns parking locations near custom coordinates (Google Places).
    /// </summary>
    [HttpGet("nearby")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
            return StatusCode(StatusCodes.Status502BadGateway,
                new { success = false, message = "Failed to reach Google Places API.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in MobilityController");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = "An unexpected error occurred." });
        }
    }
}
