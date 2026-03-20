namespace SUMMS.Api.Patterns.Observer;

public class LoggingParkingObserver : IParkingObserver
{
    private readonly ILogger<LoggingParkingObserver> _logger;

    public LoggingParkingObserver(ILogger<LoggingParkingObserver> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ParkingEvent parkingEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Parking event {EventType} for ReservationId={ReservationId}, MobilityLocationId={MobilityLocationId}, UserId={UserId}. {Message}",
            parkingEvent.EventType,
            parkingEvent.ReservationId,
            parkingEvent.MobilityLocationId,
            parkingEvent.UserId,
            parkingEvent.Message);

        return Task.CompletedTask;
    }
}
