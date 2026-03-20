using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Patterns.Command;

public class ReserveFromLocationCommand : ICommand<Reservation>
{
    private readonly IReservationService _reservationService;
    private readonly string _placeId;
    private readonly string _name;
    private readonly string _type;
    private readonly string _city;
    private readonly double _latitude;
    private readonly double _longitude;
    private readonly int _capacity;
    private readonly int _availableSpots;
    private readonly DateTime _reservationTime;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly int? _userId;

    public ReserveFromLocationCommand(
        IReservationService reservationService,
        string placeId,
        string name,
        string type,
        string city,
        double latitude,
        double longitude,
        int capacity,
        int availableSpots,
        DateTime reservationTime,
        DateTime startDate,
        DateTime endDate,
        int? userId)
    {
        _reservationService = reservationService;
        _placeId = placeId;
        _name = name;
        _type = type;
        _city = city;
        _latitude = latitude;
        _longitude = longitude;
        _capacity = capacity;
        _availableSpots = availableSpots;
        _reservationTime = reservationTime;
        _startDate = startDate;
        _endDate = endDate;
        _userId = userId;
    }

    public Task<Reservation> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _reservationService.ReserveFromLocationAsync(
            _placeId,
            _name,
            _type,
            _city,
            _latitude,
            _longitude,
            _capacity,
            _availableSpots,
            _reservationTime,
            _startDate,
            _endDate,
            _userId);
    }
}
