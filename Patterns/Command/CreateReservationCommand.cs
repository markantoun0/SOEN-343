using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Patterns.Command;

public class CreateReservationCommand : ICommand<Reservation>
{
    private readonly IReservationService _reservationService;
    private readonly int _mobilityLocationId;
    private readonly DateTime _reservationTime;
    private readonly string _city;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly string _type;
    private readonly int? _userId;

    public CreateReservationCommand(
        IReservationService reservationService,
        int mobilityLocationId,
        DateTime reservationTime,
        string city,
        DateTime startDate,
        DateTime endDate,
        string type,
        int? userId)
    {
        _reservationService = reservationService;
        _mobilityLocationId = mobilityLocationId;
        _reservationTime = reservationTime;
        _city = city;
        _startDate = startDate;
        _endDate = endDate;
        _type = type;
        _userId = userId;
    }

    public Task<Reservation> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _reservationService.InsertAsync(
            _mobilityLocationId,
            _reservationTime,
            _city,
            _startDate,
            _endDate,
            _type,
            _userId);
    }
}
