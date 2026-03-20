using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Patterns.Command;

public class DeleteReservationCommand : ICommand<bool>
{
    private readonly IReservationService _reservationService;
    private readonly int _reservationId;
    private readonly string? _deleteReason;

    public DeleteReservationCommand(
        IReservationService reservationService,
        int reservationId,
        string? deleteReason = null)
    {
        _reservationService = reservationService;
        _reservationId = reservationId;
        _deleteReason = deleteReason;
    }

    public Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _reservationService.DeleteAsync(_reservationId, _deleteReason);
    }
}
