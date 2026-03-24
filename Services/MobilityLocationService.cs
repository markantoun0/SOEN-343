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
    
    public async Task<object> GetCityAnalyticsAsync()
{
    // Fetch real data first
    var realData = await _db.MobilityLocations.ToListAsync();
    var rng = new Random();

    // Internal helper to handle both real and simulated stats
    object MapStats(int locations, int capacity, int available) {
        return new {
            totalLocations = locations,
            totalCapacity = capacity,
            totalAvailable = available,
            usageRate = capacity == 0 ? 0 : (double)(capacity - available) / capacity
        };
    }

    object BuildCity(string cityName, bool simBixi = false, bool simParking = false)
    {
        var cityRows = realData.Where(x => x.City.Equals(cityName, StringComparison.OrdinalIgnoreCase)).ToList();
        
        var bixiRow = cityRows.FirstOrDefault(x => x.Type == "bixi");
        var parkingRow = cityRows.FirstOrDefault(x => x.Type == "parking");

        // Logic for BIXI (Real or Simulated)
        var bixiStats = (bixiRow == null && simBixi) 
            ? MapStats(35, 300, rng.Next(20, 280)) // Simulated
            : bixiRow != null 
                ? MapStats(cityRows.Count(r => r.Type == "bixi"), cityRows.Where(r => r.Type == "bixi").Sum(r => r.Capacity), cityRows.Where(r => r.Type == "bixi").Sum(r => r.AvailableSpots))
                : MapStats(0, 0, 0);

        // Logic for PARKING (Real or Simulated)
        var parkingStats = (parkingRow == null && simParking)
            ? MapStats(25, 600, rng.Next(50, 550)) // Simulated
            : parkingRow != null
                ? MapStats(cityRows.Count(r => r.Type == "parking"), cityRows.Where(r => r.Type == "parking").Sum(r => r.Capacity), cityRows.Where(r => r.Type == "parking").Sum(r => r.AvailableSpots))
                : MapStats(0, 0, 0);

        // Calculate Overall City Usage
        dynamic b = bixiStats;
        dynamic p = parkingStats;
        int totalCap = b.totalCapacity + p.totalCapacity;
        int totalUsed = (b.totalCapacity - b.totalAvailable) + (p.totalCapacity - p.totalAvailable);

        return new
        {
            city = cityName,
            bixi = bixiStats,
            parking = parkingStats,
            usageRate = totalCap == 0 ? 0 : (double)totalUsed / totalCap
        };
    }

    return new
    {
        montreal = BuildCity("Montreal", simBixi: false, simParking: true),
        laval = BuildCity("Laval", simBixi: true, simParking: false),
        unknown = BuildCity("Unknown", simBixi: true, simParking: false)
    };
}

// Helper methods to keep the main logic clean
private object MapStats(dynamic g) => new {
    totalLocations = g.TotalLocations,
    totalCapacity = g.TotalCapacity,
    totalAvailable = g.TotalAvailable,
    usageRate = g.TotalCapacity == 0 ? 0 : (double)(g.TotalCapacity - g.TotalAvailable) / g.TotalCapacity
};

private object EmptyStats() => new { 
    totalLocations = 0, totalCapacity = 0, totalAvailable = 0, usageRate = 0.0 
};
}
