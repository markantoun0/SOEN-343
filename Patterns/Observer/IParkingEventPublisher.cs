namespace SUMMS.Api.Patterns.Observer;

public interface IParkingEventPublisher
{
    Task PublishAsync(ParkingEvent parkingEvent, CancellationToken cancellationToken = default);
}
