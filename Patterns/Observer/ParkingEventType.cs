namespace SUMMS.Api.Patterns.Observer;

public enum ParkingEventType
{
    ReservationCreated = 0,
    ReservationCancelled = 1,
    ReservationAboutToExpire = 2,
    ReservationExpired = 3,
    ParkingSpotAvailable = 4
}
