namespace SUMMS.Api.Patterns.Observer;

public class ParkingEventPublisher : IParkingEventPublisher
{
    private readonly IEnumerable<IParkingObserver> _observers;

    public ParkingEventPublisher(IEnumerable<IParkingObserver> observers)
    {
        _observers = observers;
    }

    public async Task PublishAsync(ParkingEvent parkingEvent, CancellationToken cancellationToken = default)
    {
        foreach (var observer in _observers)
            await observer.HandleAsync(parkingEvent, cancellationToken);
    }
}
