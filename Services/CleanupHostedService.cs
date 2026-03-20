using SUMMS.Api.Patterns.Command;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class CleanupHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupHostedService> _logger;

    public CleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        await ProcessReservationsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessReservationsAsync(stoppingToken);
        }
    }

    private async Task ProcessReservationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
            var commandInvoker = scope.ServiceProvider.GetRequiredService<ReservationCommandInvoker>();

            var expiredReservations = await commandInvoker.ExecuteAsync(
                new CleanupExpiredReservationsCommand(reservationService),
                cancellationToken);

            if (expiredReservations > 0)
                _logger.LogInformation("Reservation lifecycle sweep expired {Count} reservations", expiredReservations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reservation lifecycle sweep failed");
        }
    }
}
