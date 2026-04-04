using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Patterns.Command;
using SUMMS.Api.Patterns.Observer;
using SUMMS.Api.Services.Interfaces;
using SUMMS.Api.DTOs;
using SUMMS.Api.Services;

namespace SUMMS.Api.Tests;

public class ReservationPatternTests
{
    [Fact]
    public async Task DeleteAsync_SoftDeletesReservation_AndRestoresSpot()
    {
        await using var db = CreateDbContext();
        var location = new MobilityLocation
        {
            PlaceId = "loc-1",
            Name = "Lot A",
            Type = "parking",
            City = "Montreal",
            Latitude = 45.5,
            Longitude = -73.5,
            Capacity = 3,
            AvailableSpots = 1
        };

        var reservation = new Reservation
        {
            MobilityLocation = location,
            ReservationTime = DateTime.UtcNow.AddMinutes(-15),
            StartDate = DateTime.UtcNow.AddMinutes(-10),
            EndDate = DateTime.UtcNow.AddMinutes(30),
            City = "Montreal",
            Type = "parking",
            Status = ReservationStatus.Active
        };

        db.MobilityLocations.Add(location);
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var observer = new TestObserver();
        var service = CreateReservationService(db, observer, new FakeCarbonFootprintService());

        var deleted = await service.DeleteAsync(reservation.Id, "Cancelled in test");

        Assert.True(deleted);

        var storedReservation = await db.Reservations.SingleAsync(r => r.Id == reservation.Id);
        var storedLocation = await db.MobilityLocations.SingleAsync(l => l.Id == location.Id);

        Assert.True(storedReservation.IsDeleted);
        Assert.Equal(ReservationStatus.Cancelled, storedReservation.Status);
        Assert.Equal("Cancelled in test", storedReservation.DeleteReason);
        Assert.NotNull(storedReservation.DeletedAt);
        Assert.Equal(2, storedLocation.AvailableSpots);
        Assert.Contains(observer.Events, e => e.EventType == ParkingEventType.ReservationCancelled);
        Assert.Contains(observer.Events, e => e.EventType == ParkingEventType.ParkingSpotAvailable);
    }

    [Fact]
    public async Task CreateReservationCommand_ExecutesAndPublishesCreatedEvent()
    {
        await using var db = CreateDbContext();
        var location = new MobilityLocation
        {
            PlaceId = "loc-2",
            Name = "BIXI Hub",
            Type = "bixi",
            City = "Montreal",
            Latitude = 45.51,
            Longitude = -73.56,
            Capacity = 2,
            AvailableSpots = 2
        };

        db.MobilityLocations.Add(location);
        await db.SaveChangesAsync();

        var observer = new TestObserver();
        var carbonService = new FakeCarbonFootprintService();
        var service = CreateReservationService(db, observer, carbonService);
        var invoker = new ReservationCommandInvoker();

        var reservation = await invoker.ExecuteAsync(new CreateReservationCommand(
            service,
            location.Id,
            DateTime.UtcNow,
            "Montreal",
            DateTime.UtcNow.AddMinutes(5),
            DateTime.UtcNow.AddHours(1),
            "bixi",
            42));

        Assert.NotEqual(0, reservation.Id);
        Assert.Equal(ReservationStatus.Active, reservation.Status);

        var storedLocation = await db.MobilityLocations.SingleAsync(l => l.Id == location.Id);
        Assert.Equal(1, storedLocation.AvailableSpots);
        Assert.Contains(observer.Events, e => e.EventType == ParkingEventType.ReservationCreated);
        Assert.Contains(reservation.Id, carbonService.RecordedReservationIds);
    }

    [Fact]
    public async Task InsertAsync_DoesNotRecordCarbonSavings_ForParkingReservation()
    {
        await using var db = CreateDbContext();
        var location = new MobilityLocation
        {
            PlaceId = "loc-2-parking",
            Name = "Parking Lot",
            Type = "parking",
            City = "Montreal",
            Latitude = 45.51,
            Longitude = -73.56,
            Capacity = 2,
            AvailableSpots = 2
        };

        db.MobilityLocations.Add(location);
        await db.SaveChangesAsync();

        var carbonService = new FakeCarbonFootprintService();
        var service = CreateReservationService(db, new TestObserver(), carbonService);

        var reservation = await service.InsertAsync(
            location.Id,
            DateTime.UtcNow,
            "Montreal",
            DateTime.UtcNow.AddMinutes(5),
            DateTime.UtcNow.AddHours(1),
            "parking",
            userId: 42);

        Assert.DoesNotContain(reservation.Id, carbonService.RecordedReservationIds);
    }

    [Fact]
    public async Task CleanupExpiredReservationsAsync_ExpiresReservations_AndPublishesObserverEvents()
    {
        await using var db = CreateDbContext();
        var location = new MobilityLocation
        {
            PlaceId = "loc-3",
            Name = "Garage",
            Type = "parking",
            City = "Montreal",
            Latitude = 45.52,
            Longitude = -73.57,
            Capacity = 5,
            AvailableSpots = 2
        };

        var expiredReservation = new Reservation
        {
            MobilityLocation = location,
            ReservationTime = DateTime.UtcNow.AddHours(-2),
            StartDate = DateTime.UtcNow.AddHours(-2),
            EndDate = DateTime.UtcNow.AddMinutes(-1),
            City = "Montreal",
            Type = "parking",
            Status = ReservationStatus.Active
        };

        var expiringSoonReservation = new Reservation
        {
            MobilityLocation = location,
            ReservationTime = DateTime.UtcNow.AddHours(-1),
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddMinutes(5),
            City = "Montreal",
            Type = "parking",
            Status = ReservationStatus.Active
        };

        db.MobilityLocations.Add(location);
        db.Reservations.AddRange(expiredReservation, expiringSoonReservation);
        await db.SaveChangesAsync();

        var observer = new TestObserver();
        var service = CreateReservationService(db, observer, new FakeCarbonFootprintService());

        var cleanedCount = await service.CleanupExpiredReservationsAsync();

        Assert.Equal(1, cleanedCount);

        var storedExpired = await db.Reservations.SingleAsync(r => r.Id == expiredReservation.Id);
        var storedSoon = await db.Reservations.SingleAsync(r => r.Id == expiringSoonReservation.Id);
        var storedLocation = await db.MobilityLocations.SingleAsync(l => l.Id == location.Id);

        Assert.True(storedExpired.IsDeleted);
        Assert.Equal(ReservationStatus.Expired, storedExpired.Status);
        Assert.NotNull(storedSoon.ExpirationWarningSentAt);
        Assert.Equal(3, storedLocation.AvailableSpots);

        Assert.Contains(observer.Events, e => e.EventType == ParkingEventType.ReservationAboutToExpire);
        Assert.Contains(observer.Events, e => e.EventType == ParkingEventType.ReservationExpired);
        Assert.Contains(observer.Events, e => e.EventType == ParkingEventType.ParkingSpotAvailable);
    }

    [Fact]
    public async Task InsertAsync_ConvertsLocalDatesToUtc()
    {
        await using var db = CreateDbContext();
        var location = new MobilityLocation
        {
            PlaceId = "loc-utc",
            Name = "UTC Test Lot",
            Type = "parking",
            City = "Montreal",
            Latitude = 45.5,
            Longitude = -73.5,
            Capacity = 2,
            AvailableSpots = 2
        };

        db.MobilityLocations.Add(location);
        await db.SaveChangesAsync();

        var service = CreateReservationService(db, new TestObserver(), new FakeCarbonFootprintService());
        var localReservationTime = DateTime.SpecifyKind(new DateTime(2026, 4, 2, 9, 0, 0), DateTimeKind.Local);
        var localStart = DateTime.SpecifyKind(new DateTime(2026, 4, 2, 10, 0, 0), DateTimeKind.Local);
        var localEnd = DateTime.SpecifyKind(new DateTime(2026, 4, 2, 11, 0, 0), DateTimeKind.Local);

        var reservation = await service.InsertAsync(
            location.Id,
            localReservationTime,
            "Montreal",
            localStart,
            localEnd,
            "parking",
            userId: 1);

        Assert.Equal(DateTimeKind.Utc, reservation.ReservationTime.Kind);
        Assert.Equal(DateTimeKind.Utc, reservation.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, reservation.EndDate.Kind);
        Assert.Equal(localReservationTime.ToUniversalTime(), reservation.ReservationTime);
        Assert.Equal(localStart.ToUniversalTime(), reservation.StartDate);
        Assert.Equal(localEnd.ToUniversalTime(), reservation.EndDate);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ReservationService CreateReservationService(
        AppDbContext db,
        IParkingObserver observer,
        ICarbonFootprintService carbonFootprintService)
    {
        var publisher = new ParkingEventPublisher(new[] { observer });
        return new ReservationService(db, NullLogger<ReservationService>.Instance, publisher, carbonFootprintService);
    }

    private sealed class FakeCarbonFootprintService : ICarbonFootprintService
    {
        public List<int> RecordedReservationIds { get; } = [];

        public Task<CarbonFootprintDto?> GetUserCarbonFootprintAsync(int userId)
            => Task.FromResult<CarbonFootprintDto?>(null);

        public Task<TripCarbonFootprintDto> RecordBixiSavingsForReservationAsync(int reservationId)
        {
            RecordedReservationIds.Add(reservationId);
            return Task.FromResult(new TripCarbonFootprintDto
            {
                ReservationId = reservationId,
                MobilityType = "bixi",
                DurationMinutes = 60,
                EstimatedSavedKg = 0
            });
        }

        public Task<List<UserLeaderboardEntryDto>> GetLeaderboardAsync(int topN = 10)
            => Task.FromResult(new List<UserLeaderboardEntryDto>());

        public Task<int?> GetUserRankAsync(int userId)
            => Task.FromResult<int?>(null);
    }

    private sealed class TestObserver : IParkingObserver
    {
        public List<ParkingEvent> Events { get; } = [];

        public Task HandleAsync(ParkingEvent parkingEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(parkingEvent);
            return Task.CompletedTask;
        }
    }
}