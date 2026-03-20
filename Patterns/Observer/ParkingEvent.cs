namespace SUMMS.Api.Patterns.Observer;

public sealed record ParkingEvent(
    ParkingEventType EventType,
    int ReservationId,
    int MobilityLocationId,
    int? UserId,
    string Message,
    DateTime OccurredAtUtc);
