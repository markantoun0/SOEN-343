namespace SUMMS.Api.Patterns.Observer;

public interface IParkingObserver
{
    Task HandleAsync(ParkingEvent parkingEvent, CancellationToken cancellationToken = default);
}
