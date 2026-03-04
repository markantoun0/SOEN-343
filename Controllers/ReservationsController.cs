using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(IReservationService reservationService, ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _logger             = logger;
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
            var reservation = await _reservationService.InsertAsync(
                mobilityLocationId: request.MobilityLocationId,
                reservationTime:    request.ReservationTime,
                city:               request.City,
                type:               request.Type);

            return CreatedAtAction(
                nameof(GetById),
                new { id = reservation.Id },
                new { success = true, reservation });
        }
        catch (InvalidOperationException ex)
        {
            // Thrown when the linked MobilityLocation does not exist
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
            var reservation = await _reservationService.ReserveFromLocationAsync(
                placeId: request.PlaceId,
                name: request.Name,
                type: request.Type,
                city: request.City,
                latitude: request.Latitude,
                longitude: request.Longitude,
                capacity: request.Capacity,
                availableSpots: request.AvailableSpots,
                reservationTime: request.ReservationTime);

            return CreatedAtAction(
                nameof(GetById),
                new { id = reservation.Id },
                new { success = true, reservation });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _reservationService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { success = false, message = $"No reservation found with Id={id}." });

        return Ok(new { success = true, message = $"Reservation Id={id} deleted." });
    }
}

public class CreateReservationRequest
{
    public int      MobilityLocationId { get; set; }
    public DateTime ReservationTime    { get; set; } = DateTime.UtcNow;
    public string   City               { get; set; } = string.Empty;
    public string   Type               { get; set; } = string.Empty;
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
}
