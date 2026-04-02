using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.DTOs;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class CarbonFootprintService : ICarbonFootprintService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CarbonFootprintService> _logger;

    // Carbon emissions factors in kg CO2 per km for different mobility types
    private static readonly Dictionary<string, double> CarbonEmissionFactors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "bixi", 0.0 },              // Zero emissions - bicycle
        { "parking", 0.21 },           // Car average ~210g CO2 per km
        { "car", 0.21 },
        { "bus", 0.089 },              // Public transport ~89g CO2 per km per passenger
        { "scooter", 0.022 }           // E-scooter ~22g CO2 per km
    };

    public CarbonFootprintService(AppDbContext db, ILogger<CarbonFootprintService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CarbonFootprintDto?> GetUserCarbonFootprintAsync(int userId)
    {
        _logger.LogInformation("Getting carbon footprint for user {UserId}", userId);

        var footprint = await _db.CarbonFootprints
            .Include(cf => cf.User)
            .FirstOrDefaultAsync(cf => cf.UserId == userId);

        if (footprint == null)
            return null;

        return new CarbonFootprintDto
        {
            UserId = footprint.UserId,
            UserName = footprint.User?.Name ?? "Unknown",
            TotalCarbonKg = footprint.TotalCarbonKg,
            TripsCompleted = footprint.TripsCompleted,
            LastUpdated = footprint.LastUpdated
        };
    }

    public async Task<TripCarbonFootprintDto> CalculateTripCarbonFootprintAsync(int reservationId, double distanceKm)
    {
        _logger.LogInformation("Calculating carbon footprint for reservation {ReservationId}, distance {DistanceKm}km", reservationId, distanceKm);

        var reservation = await _db.Reservations
            .Include(r => r.MobilityLocation)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            throw new InvalidOperationException($"Reservation {reservationId} not found");

        if (reservation.UserId == null)
            throw new InvalidOperationException("Reservation does not have an associated user");

        var mobilityType = reservation.Type ?? reservation.MobilityLocation?.Type ?? "unknown";
        var emissionFactor = GetEmissionFactor(mobilityType);
        var carbonKg = distanceKm * emissionFactor;

        // Calculate duration in minutes
        var duration = (reservation.EndDate - reservation.StartDate).TotalMinutes;

        // Update or create carbon footprint record
        var footprint = await _db.CarbonFootprints
            .FirstOrDefaultAsync(cf => cf.UserId == reservation.UserId);

        if (footprint == null)
        {
            footprint = new CarbonFootprint
            {
                UserId = reservation.UserId.Value,
                TotalCarbonKg = carbonKg,
                TripsCompleted = 1,
                LastUpdated = DateTime.UtcNow
            };
            _db.CarbonFootprints.Add(footprint);
        }
        else
        {
            footprint.TotalCarbonKg += carbonKg;
            footprint.TripsCompleted += 1;
            footprint.LastUpdated = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Carbon footprint updated for user {UserId}: +{CarbonKg}kg CO2", reservation.UserId, carbonKg);

        return new TripCarbonFootprintDto
        {
            ReservationId = reservationId,
            MobilityType = mobilityType,
            DistanceKm = distanceKm,
            DurationMinutes = duration,
            CarbonKg = carbonKg
        };
    }

    public async Task<List<UserLeaderboardEntryDto>> GetLeaderboardAsync(int topN = 10)
    {
        _logger.LogInformation("Getting top {TopN} users by lowest carbon emissions", topN);

        var leaderboard = await _db.CarbonFootprints
            .Include(cf => cf.User)
            .OrderBy(cf => cf.TotalCarbonKg)
            .Take(topN)
            .ToListAsync();

        var result = new List<UserLeaderboardEntryDto>();
        int rank = 1;

        foreach (var footprint in leaderboard)
        {
            result.Add(new UserLeaderboardEntryDto
            {
                Rank = rank++,
                UserId = footprint.UserId,
                UserName = footprint.User?.Name ?? "Unknown",
                TotalCarbonKg = footprint.TotalCarbonKg,
                TripsCompleted = footprint.TripsCompleted
            });
        }

        return result;
    }

    public async Task<int?> GetUserRankAsync(int userId)
    {
        _logger.LogInformation("Getting rank for user {UserId}", userId);

        var userFootprint = await _db.CarbonFootprints
            .FirstOrDefaultAsync(cf => cf.UserId == userId);

        if (userFootprint == null)
            return null;

        var rankPosition = await _db.CarbonFootprints
            .Where(cf => cf.TotalCarbonKg < userFootprint.TotalCarbonKg)
            .CountAsync();

        return rankPosition + 1;
    }

    private double GetEmissionFactor(string mobilityType)
    {
        if (CarbonEmissionFactors.TryGetValue(mobilityType, out var factor))
            return factor;

        // Default to car emissions if type not found
        _logger.LogWarning("Unknown mobility type {MobilityType}, using default car emissions", mobilityType);
        return CarbonEmissionFactors["car"];
    }
}

