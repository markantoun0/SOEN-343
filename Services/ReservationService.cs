using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(AppDbContext db, ILogger<ReservationService> logger)
    {
        _db     = db;
        _logger = logger;
    }


    public async Task<IEnumerable<Reservation>> GetAllAsync(string? type = null, string? city = null)
    {
        _logger.LogInformation("Retrieving reservations (type={Type}, city={City})", type ?? "any", city ?? "any");
        

        var query = _db.Reservations
                       .Include(r => r.MobilityLocation) // eager-load the linked location
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

        return await _db.Reservations
                        .Include(r => r.MobilityLocation)
                        .Where(r => r.MobilityLocationId == mobilityLocationId)
                        .ToListAsync();
    }


    public async Task<Reservation> InsertAsync(
        int      mobilityLocationId,
        DateTime reservationTime,
        string   city,
        DateTime startDate, // Added
        DateTime endDate,   // Added
        string   type)
    {
        // 1. Basic Validation
        if (endDate <= startDate)
            throw new InvalidOperationException("End date must be after start date.");
        
        

        var location = await _db.MobilityLocations.FirstOrDefaultAsync(l => l.Id == mobilityLocationId);
        if (location == null)
            throw new InvalidOperationException($"MobilityLocation with Id={mobilityLocationId} does not exist.");
        
        if (location != null && location.AvailableSpots > 0) {
            location.AvailableSpots -= 1; 
        }

        // 2. Overlap Check
        // A spot is occupied if an existing reservation overlaps with the requested range.
        var overlappingCount = await _db.Reservations.CountAsync(r =>
            r.MobilityLocationId == mobilityLocationId &&
            r.StartDate < endDate && 
            r.EndDate > startDate);

        if (overlappingCount >= location.Capacity)
            throw new InvalidOperationException("No spots available for the selected time range.");

        var reservation = new Reservation
        {
            MobilityLocationId = mobilityLocationId,
            ReservationTime    = DateTime.SpecifyKind(reservationTime, DateTimeKind.Utc),
            StartDate          = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate            = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            City               = city,
            Type               = type
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        await _db.Entry(reservation).Reference(r => r.MobilityLocation).LoadAsync();
        return reservation;
    }

    public async Task<Reservation> ReserveFromLocationAsync(
        string   placeId,
        string   name,
        string   type,
        string   city,
        double   latitude,
        double   longitude,
        int      capacity,
        int      availableSpots,
        DateTime reservationTime,
        DateTime startDate, // Added
        DateTime endDate)   // Added
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? "unknown" : type.Trim().ToLowerInvariant();
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? "Unknown" : city.Trim();
    
        var location = await _db.MobilityLocations.FirstOrDefaultAsync(l => l.PlaceId == placeId);

        if (location is null)
        {
            location = new MobilityLocation
            {
                PlaceId = placeId,
                Name = name,
                Type = normalizedType,
                City = normalizedCity,
                Latitude = latitude,
                Longitude = longitude,
                Capacity = Math.Max(1, capacity), // Ensure at least 1 spot exists
                AvailableSpots = availableSpots 
            };
            _db.MobilityLocations.Add(location);
            await _db.SaveChangesAsync();
        }

        // Instead of decrementing AvailableSpots globally, 
        // we let InsertAsync check the schedule for this specific time slot.
        return await InsertAsync(
            mobilityLocationId: location.Id,
            reservationTime: reservationTime,
            city: normalizedCity,
            startDate: startDate,
            endDate: endDate,
            type: normalizedType);
    }


    public async Task<bool> DeleteAsync(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation is null)
        {
            _logger.LogWarning("Delete: Reservation Id={Id} not found", id);
            return false;
        }

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted Reservation Id={Id}", id);
        return true;
    }
    
    public async Task<int> CleanupExpiredReservationsAsync()
    {
        var now = DateTime.UtcNow;

        // 1. Fetch reservations that have ended
        var expiredReservations = await _db.Reservations
            .Include(r => r.MobilityLocation)
            .Where(r => r.EndDate <= now)
            .ToListAsync();

        if (!expiredReservations.Any())
        {
            return 0;
        }

        foreach (var reservation in expiredReservations)
        {
            _logger.LogInformation("Cleaning up expired reservation Id={ResId} for Location={LocId}", 
                reservation.Id, reservation.MobilityLocationId);

            // 2. Add the spot back if the location exists
            if (reservation.MobilityLocation != null)
            {
                // Ensure we don't exceed the maximum capacity
                reservation.MobilityLocation.AvailableSpots = Math.Min(
                    reservation.MobilityLocation.Capacity, 
                    reservation.MobilityLocation.AvailableSpots + 1
                );
            }

            // 3. Remove the record
            _db.Reservations.Remove(reservation);
        }

        await _db.SaveChangesAsync();
        return expiredReservations.Count;
    }
}
