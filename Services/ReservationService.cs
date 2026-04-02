using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Patterns.Observer;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReservationService> _logger;
    private readonly IParkingEventPublisher _eventPublisher;

    public ReservationService(
        AppDbContext db,
        ILogger<ReservationService> logger,
        IParkingEventPublisher eventPublisher)
    {
        _db = db;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task<IEnumerable<Reservation>> GetAllAsync(string? type = null, string? city = null)
    {
        _logger.LogInformation("Retrieving reservations (type={Type}, city={City})", type ?? "any", city ?? "any");

        var query = ActiveReservations()
            .Include(r => r.MobilityLocation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(r => r.Type == type);

        if (!string.IsNullOrEmpty(city))
            query = query.Where(r => r.City == city);

        return await query.ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving reservation Id={Id}", id);
        return await _db.Reservations
            .Include(r => r.MobilityLocation)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reservation>> GetByLocationIdAsync(int mobilityLocationId)
    {
        _logger.LogInformation("Retrieving reservations for MobilityLocationId={Id}", mobilityLocationId);
        return await ActiveReservations()
            .Include(r => r.MobilityLocation)
            .Where(r => r.MobilityLocationId == mobilityLocationId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId)
    {
        _logger.LogInformation("Retrieving reservations for UserId={Id}", userId);
        return await _db.Reservations
            .Include(r => r.MobilityLocation)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();
    }

    public async Task<Reservation> InsertAsync(
        int mobilityLocationId,
        DateTime reservationTime,
        string city,
        DateTime startDate,
        DateTime endDate,
        string type,
        int? userId = null)
    {
        if (endDate <= startDate)
            throw new InvalidOperationException("End date must be after start date.");

        var location = await _db.MobilityLocations.FirstOrDefaultAsync(l => l.Id == mobilityLocationId);
        if (location is null)
            throw new InvalidOperationException($"MobilityLocation with Id={mobilityLocationId} does not exist.");

        var overlappingCount = await ActiveReservations().CountAsync(r =>
            r.MobilityLocationId == mobilityLocationId &&
            r.StartDate < endDate &&
            r.EndDate > startDate);

        if (overlappingCount >= location.Capacity || location.AvailableSpots <= 0)
            throw new InvalidOperationException("No spots available for the selected time range.");

        location.AvailableSpots -= 1;

        var reservation = new Reservation
        {
            MobilityLocationId = mobilityLocationId,
            ReservationTime = NormalizeToUtc(reservationTime),
            StartDate = NormalizeToUtc(startDate),
            EndDate = NormalizeToUtc(endDate),
            City = city,
            Type = type,
            Status = ReservationStatus.Active,
            UserId = userId
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        await _db.Entry(reservation).Reference(r => r.MobilityLocation).LoadAsync();
        await PublishReservationEventAsync(
            ParkingEventType.ReservationCreated,
            reservation,
            $"Reservation {reservation.Id} created for location {reservation.MobilityLocationId}.");

        return reservation;
    }

    public async Task<Reservation> ReserveFromLocationAsync(
        string placeId,
        string name,
        string type,
        string city,
        double latitude,
        double longitude,
        int capacity,
        int availableSpots,
        DateTime reservationTime,
        DateTime startDate,
        DateTime endDate,
        int? userId = null)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? "unknown" : type.Trim().ToLowerInvariant();
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? "Unknown" : city.Trim();

        var location = await _db.MobilityLocations.FirstOrDefaultAsync(l => l.PlaceId == placeId);

        if (location is null)
        {
            // When creating a new location, use the capacity from the API
            // Ensure capacity is at least as large as availableSpots to prevent data corruption
            var initialCapacity = Math.Max(Math.Max(1, capacity), availableSpots);
            location = new MobilityLocation
            {
                PlaceId = placeId,
                Name = name,
                Type = normalizedType,
                City = normalizedCity,
                Latitude = latitude,
                Longitude = longitude,
                Capacity = initialCapacity,
                AvailableSpots = Math.Min(availableSpots, initialCapacity)
            };
            _db.MobilityLocations.Add(location);
            await _db.SaveChangesAsync();
        }

        return await InsertAsync(
            mobilityLocationId: location.Id,
            reservationTime: reservationTime,
            city: normalizedCity,
            startDate: startDate,
            endDate: endDate,
            type: normalizedType,
            userId: userId);
    }

    public async Task<bool> DeleteAsync(int id, string? deleteReason = null)
    {
        var reservation = await _db.Reservations
            .Include(r => r.MobilityLocation)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            _logger.LogWarning("Delete: Reservation Id={Id} not found", id);
            return false;
        }

        if (!CanReleaseSpot(reservation))
        {
            _logger.LogInformation(
                "Delete ignored for Reservation Id={Id} because it is already {Status}",
                reservation.Id,
                reservation.Status);
            return true;
        }

        RestoreSpot(reservation);
        MarkReservationInactive(reservation, ReservationStatus.Cancelled, deleteReason ?? "Cancelled by user");

        await _db.SaveChangesAsync();
        await PublishReservationEventAsync(
            ParkingEventType.ReservationCancelled,
            reservation,
            $"Reservation {reservation.Id} cancelled.");
        await PublishParkingSpotAvailableAsync(reservation);

        _logger.LogInformation("Soft-deleted Reservation Id={Id}", id);
        return true;
    }

    public async Task<int> CleanupExpiredReservationsAsync()
    {
        var now = DateTime.UtcNow;
        var warningThreshold = now.AddMinutes(10);

        var expiringSoonReservations = await ActiveReservations()
            .Include(r => r.MobilityLocation)
            .Where(r => r.EndDate > now &&
                        r.EndDate <= warningThreshold &&
                        r.ExpirationWarningSentAt == null)
            .ToListAsync();

        foreach (var reservation in expiringSoonReservations)
        {
            reservation.ExpirationWarningSentAt = now;
            await PublishReservationEventAsync(
                ParkingEventType.ReservationAboutToExpire,
                reservation,
                $"Reservation {reservation.Id} will expire at {reservation.EndDate:u}.");
        }

        var expiredReservations = await ActiveReservations()
            .Include(r => r.MobilityLocation)
            .Where(r => r.EndDate <= now)
            .ToListAsync();

        foreach (var reservation in expiredReservations)
        {
            _logger.LogInformation(
                "Cleaning up expired reservation Id={ReservationId} for Location={LocationId}",
                reservation.Id,
                reservation.MobilityLocationId);

            RestoreSpot(reservation);
            MarkReservationInactive(reservation, ReservationStatus.Expired, "Expired automatically");
        }

        if (!expiringSoonReservations.Any() && !expiredReservations.Any())
            return 0;

        await _db.SaveChangesAsync();

        foreach (var reservation in expiredReservations)
        {
            await PublishReservationEventAsync(
                ParkingEventType.ReservationExpired,
                reservation,
                $"Reservation {reservation.Id} expired.");
            await PublishParkingSpotAvailableAsync(reservation);
        }

        return expiredReservations.Count;
    }

    private IQueryable<Reservation> ActiveReservations()
    {
        return _db.Reservations.Where(r => !r.IsDeleted && r.Status == ReservationStatus.Active);
    }

    private static bool CanReleaseSpot(Reservation reservation)
    {
        return !reservation.IsDeleted && reservation.Status == ReservationStatus.Active;
    }

    private static void RestoreSpot(Reservation reservation)
    {
        if (reservation.MobilityLocation is null)
            return;

        reservation.MobilityLocation.AvailableSpots = Math.Min(
            reservation.MobilityLocation.Capacity,
            reservation.MobilityLocation.AvailableSpots + 1);
    }

    private static void MarkReservationInactive(
        Reservation reservation,
        ReservationStatus status,
        string reason)
    {
        reservation.Status = status;
        reservation.IsDeleted = true;
        reservation.DeletedAt = DateTime.UtcNow;
        reservation.DeleteReason = reason;
    }

    private Task PublishReservationEventAsync(
        ParkingEventType eventType,
        Reservation reservation,
        string message)
    {
        return _eventPublisher.PublishAsync(new ParkingEvent(
            eventType,
            reservation.Id,
            reservation.MobilityLocationId,
            reservation.UserId,
            message,
            DateTime.UtcNow));
    }

    private Task PublishParkingSpotAvailableAsync(Reservation reservation)
    {
        if (reservation.MobilityLocation is null)
            return Task.CompletedTask;

        return _eventPublisher.PublishAsync(new ParkingEvent(
            ParkingEventType.ParkingSpotAvailable,
            reservation.Id,
            reservation.MobilityLocationId,
            reservation.UserId,
            $"{reservation.MobilityLocation.Name} now has {reservation.MobilityLocation.AvailableSpots} available spots.",
            DateTime.UtcNow));
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
