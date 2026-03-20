using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Patterns.Command;

public class CleanupExpiredReservationsCommand : ICommand<int>
{
    private readonly IReservationService _reservationService;

    public CleanupExpiredReservationsCommand(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _reservationService.CleanupExpiredReservationsAsync();
    }
}
