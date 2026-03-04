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
        string   type)
    {
        var locationExists = await _db.MobilityLocations.AnyAsync(l => l.Id == mobilityLocationId);
        if (!locationExists)
            throw new InvalidOperationException($"MobilityLocation with Id={mobilityLocationId} does not exist.");

        var reservation = new Reservation
        {
            MobilityLocationId = mobilityLocationId,
            ReservationTime    = DateTime.SpecifyKind(reservationTime, DateTimeKind.Utc),
            City               = city,
            Type               = type
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        // Reload with navigation so the caller gets the full object
        await _db.Entry(reservation).Reference(r => r.MobilityLocation).LoadAsync();

        _logger.LogInformation(
            "Inserted Reservation Id={Id} for MobilityLocationId={LocationId}",
            reservation.Id, reservation.MobilityLocationId);

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
        DateTime reservationTime)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? "unknown" : type.Trim().ToLowerInvariant();
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? "Unknown" : city.Trim();
        var safeSpots = Math.Max(0, availableSpots);
        if (safeSpots <= 0)
            throw new InvalidOperationException("There are no spots anymore.");
        var decrementedRequestedSpots = Math.Max(0, safeSpots - 1);
        var safeCapacity = Math.Max(Math.Max(0, capacity), safeSpots);

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
                Capacity = safeCapacity,
                AvailableSpots = decrementedRequestedSpots
            };

            _db.MobilityLocations.Add(location);
        }
        else
        {
            if (location.AvailableSpots <= 0)
                throw new InvalidOperationException("There are no spots anymore.");

            location.Name = name;
            location.Type = normalizedType;
            location.City = normalizedCity;
            location.Latitude = latitude;
            location.Longitude = longitude;
            location.Capacity = safeCapacity;
            location.AvailableSpots = Math.Max(0, location.AvailableSpots - 1);
        }

        await _db.SaveChangesAsync();

        return await InsertAsync(
            mobilityLocationId: location.Id,
            reservationTime: reservationTime,
            city: normalizedCity,
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
}
