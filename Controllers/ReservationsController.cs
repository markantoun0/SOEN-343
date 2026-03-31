using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Patterns.Command;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ReservationCommandInvoker _commandInvoker;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        IReservationService reservationService,
        ReservationCommandInvoker commandInvoker,
        ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _commandInvoker = commandInvoker;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type = null,
        [FromQuery] string? city = null)
    {
        var reservations = (await _reservationService.GetAllAsync(type, city)).ToList();
        return Ok(new { success = true, count = reservations.Count, reservations });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var reservation = await _reservationService.GetByIdAsync(id);
        if (reservation is null)
            return NotFound(new { success = false, message = $"No reservation found with Id={id}." });

        return Ok(new { success = true, reservation });
    }

    [HttpGet("by-location/{locationId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLocation(int locationId)
    {
        var reservations = (await _reservationService.GetByLocationIdAsync(locationId)).ToList();
        return Ok(new { success = true, count = reservations.Count, reservations });
    }

    [HttpGet("by-user/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var reservations = (await _reservationService.GetByUserIdAsync(userId)).ToList();
        return Ok(new { success = true, count = reservations.Count, reservations });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { success = false, message = "Type is required." });

        try
        {
            var command = new CreateReservationCommand(
                _reservationService,
                request.MobilityLocationId,
                request.ReservationTime,
                request.City,
                request.StartDate,
                request.EndDate,
                request.Type,
                request.UserId);

            var reservation = await _commandInvoker.ExecuteAsync(command);

            return CreatedAtAction(
                nameof(GetById),
                new { id = reservation.Id },
                new { success = true, reservation });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Reservation creation failed");
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("reserve-location")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReserveFromLocation([FromBody] ReserveFromLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlaceId) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest(new { success = false, message = "PlaceId, Name, and Type are required." });
        }

        try
        {
            var command = new ReserveFromLocationCommand(
                _reservationService,
                request.PlaceId,
                request.Name,
                request.Type,
                request.City,
                request.Latitude,
                request.Longitude,
                request.Capacity,
                request.AvailableSpots,
                request.ReservationTime,
                request.StartDate,
                request.EndDate,
                request.UserId);

            var reservation = await _commandInvoker.ExecuteAsync(command);

            return CreatedAtAction(
                nameof(GetById),
                new { id = reservation.Id },
                new { success = true, reservation });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Reserve-from-location failed");
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteReservationCommand(_reservationService, id, "Cancelled by user");
        var deleted = await _commandInvoker.ExecuteAsync(command);

        if (!deleted)
            return NotFound(new { success = false, message = $"No reservation found with Id={id}." });

        return Ok(new { success = true, message = $"Reservation Id={id} cancelled." });
    }

    [HttpPost("cleanup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cleanup()
    {
        try
        {
            var cleanedUpCount = await _reservationService.CleanupExpiredReservationsAsync();
            return Ok(new { success = true, message = $"Cleaned up {cleanedUpCount} expired reservations." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup failed");
            return StatusCode(500, new { success = false, message = "Cleanup operation failed." });
        }
    }
}

public class CreateReservationRequest
{
    public int MobilityLocationId { get; set; }
    public DateTime ReservationTime { get; set; } = DateTime.UtcNow;
    public string City { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UserId { get; set; }
}

public class ReserveFromLocationRequest
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Capacity { get; set; }
    public int AvailableSpots { get; set; }
    public DateTime ReservationTime { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UserId { get; set; }
}
