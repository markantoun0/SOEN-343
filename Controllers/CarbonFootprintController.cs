using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarbonFootprintController : ControllerBase
{
    private readonly ICarbonFootprintService _carbonFootprintService;
    private readonly ILogger<CarbonFootprintController> _logger;

    public CarbonFootprintController(ICarbonFootprintService carbonFootprintService, ILogger<CarbonFootprintController> logger)
    {
        _carbonFootprintService = carbonFootprintService;
        _logger = logger;
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserCarbonFootprint(int userId)
    {
        _logger.LogInformation("Getting carbon footprint for user {UserId}", userId);

        var footprint = await _carbonFootprintService.GetUserCarbonFootprintAsync(userId);
        if (footprint == null)
            return NotFound(new { success = false, message = "No carbon footprint data found for this user" });

        return Ok(new { success = true, data = footprint });
    }

    [HttpPost("calculate-trip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateTripCarbonFootprint([FromBody] CalculateTripRequest request)
    {
        if (request.ReservationId <= 0 || request.DistanceKm < 0)
            return BadRequest(new { success = false, message = "Invalid reservation ID or distance" });

        try
        {
            var tripFootprint = await _carbonFootprintService.CalculateTripCarbonFootprintAsync(request.ReservationId, request.DistanceKm);
            return Ok(new { success = true, data = tripFootprint });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int topN = 10)
    {
        if (topN <= 0 || topN > 100)
            topN = 10;

        _logger.LogInformation("Getting leaderboard for top {TopN} users", topN);

        var leaderboard = await _carbonFootprintService.GetLeaderboardAsync(topN);
        return Ok(new { success = true, data = leaderboard });
    }

    [HttpGet("rank/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRank(int userId)
    {
        _logger.LogInformation("Getting rank for user {UserId}", userId);

        var rank = await _carbonFootprintService.GetUserRankAsync(userId);
        if (rank == null)
            return NotFound(new { success = false, message = "User not found in rankings" });

        return Ok(new { success = true, data = new { userId, rank } });
    }
}

public class CalculateTripRequest
{
    public int ReservationId { get; set; }
    public double DistanceKm { get; set; }
}

