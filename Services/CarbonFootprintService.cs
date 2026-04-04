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

    // Estimated baseline for equivalent car travel in city traffic.
    private const double CarEmissionKgPerKm = 0.21;
    private const double EstimatedUrbanCarKmPerHour = 25.0;

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
            TotalSavedKg = footprint.TotalCarbonKg,
            TripsCompleted = footprint.TripsCompleted,
            LastUpdated = footprint.LastUpdated
        };
    }

    public async Task<TripCarbonFootprintDto> RecordBixiSavingsForReservationAsync(int reservationId)
    {
        _logger.LogInformation("Calculating BIXI emissions savings for reservation {ReservationId}", reservationId);

        var reservation = await _db.Reservations
            .Include(r => r.MobilityLocation)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            throw new InvalidOperationException($"Reservation {reservationId} not found");

        if (reservation.UserId == null)
            throw new InvalidOperationException("Reservation does not have an associated user");

        var mobilityType = reservation.Type ?? reservation.MobilityLocation?.Type ?? "unknown";
        if (!string.Equals(mobilityType, "bixi", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only BIXI reservations are eligible for emissions savings estimation.");

        var duration = (reservation.EndDate - reservation.StartDate).TotalMinutes;
        if (duration <= 0)
            throw new InvalidOperationException("Reservation duration must be greater than zero.");

        var estimatedDistanceKm = (duration / 60.0) * EstimatedUrbanCarKmPerHour;
        var savedKg = estimatedDistanceKm * CarEmissionKgPerKm;

        var footprint = await _db.CarbonFootprints
            .FirstOrDefaultAsync(cf => cf.UserId == reservation.UserId);

        if (footprint == null)
        {
            footprint = new CarbonFootprint
            {
                UserId = reservation.UserId.Value,
                TotalCarbonKg = savedKg,
                TripsCompleted = 1,
                LastUpdated = DateTime.UtcNow
            };
            _db.CarbonFootprints.Add(footprint);
        }
        else
        {
            footprint.TotalCarbonKg += savedKg;
            footprint.TripsCompleted += 1;
            footprint.LastUpdated = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Carbon savings updated for user {UserId}: +{SavedKg}kg CO2 saved", reservation.UserId, savedKg);

        return new TripCarbonFootprintDto
        {
            ReservationId = reservationId,
            MobilityType = mobilityType,
            DurationMinutes = duration,
            EstimatedSavedKg = savedKg
        };
    }

    public async Task<List<UserLeaderboardEntryDto>> GetLeaderboardAsync(int topN = 10)
    {
        _logger.LogInformation("Getting top {TopN} users by highest emissions saved", topN);

        var leaderboard = await _db.CarbonFootprints
            .Include(cf => cf.User)
            .OrderByDescending(cf => cf.TotalCarbonKg)
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
                TotalSavedKg = footprint.TotalCarbonKg,
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
            .Where(cf => cf.TotalCarbonKg > userFootprint.TotalCarbonKg)
            .CountAsync();

        return rankPosition + 1;
    }
}
