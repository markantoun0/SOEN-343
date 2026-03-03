using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class MobilityLocationService : IMobilityLocationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MobilityLocationService> _logger;

    public MobilityLocationService(AppDbContext db, ILogger<MobilityLocationService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IEnumerable<MobilityLocation>> GetAllAsync(string? type = null)
    {
        _logger.LogInformation("Retrieving all locations (type filter: {Type})", type ?? "none");

        var query = _db.MobilityLocations.AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(l => l.Type == type);

        return await query.ToListAsync();
    }

    public async Task<MobilityLocation?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving location with Id={Id}", id);

        return await _db.MobilityLocations.FindAsync(id);
    }

    public async Task<MobilityLocation> InsertAsync(
        string placeId,
        string name,
        string type,
        string city,
        double latitude,
        double longitude,
        int capacity,
        int availableSpots)
    {
        var location = new MobilityLocation
        {
            PlaceId        = placeId,
            Name           = name,
            Type           = type,
            City           = city,
            Latitude       = latitude,
            Longitude      = longitude,
            Capacity       = capacity,
            AvailableSpots = availableSpots
        };

        _db.MobilityLocations.Add(location);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Inserted MobilityLocation Id={Id} Name={Name}", location.Id, location.Name);

        return location;  // Id is now populated by the database
    }

    public async Task<MobilityLocation?> UpdateAvailableSpotsAsync(int id, int availableSpots)
    {
        var location = await _db.MobilityLocations.FindAsync(id);

        if (location is null)
        {
            _logger.LogWarning("UpdateAvailableSpots: Id={Id} not found", id);
            return null;
        }

        location.AvailableSpots = availableSpots;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated AvailableSpots for Id={Id} to {Spots}", id, availableSpots);

        return location;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var location = await _db.MobilityLocations.FindAsync(id);

        if (location is null)
        {
            _logger.LogWarning("Delete: Id={Id} not found", id);
            return false;
        }

        _db.MobilityLocations.Remove(location);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted MobilityLocation Id={Id}", id);

        return true;
    }
}
