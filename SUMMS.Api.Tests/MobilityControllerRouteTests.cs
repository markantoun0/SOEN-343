using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SUMMS.Api.Controllers;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Tests;

public class MobilityControllerRouteTests
{
    [Fact]
    public async Task ComputeRoute_ReturnsOk_ForValidCarRequest()
    {
        var controller = CreateController(new FakeRouteService());

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "Montreal, QC",
            Destination = "Laval, QC",
            TravelMode = "car"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task ComputeRoute_ReturnsOk_ForValidBikeRequest()
    {
        var controller = CreateController(new FakeRouteService());

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "Montreal, QC",
            Destination = "Laval, QC",
            TravelMode = "bike"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task ComputeRoute_ReturnsBadRequest_WhenOriginIsEmpty()
    {
        var controller = CreateController(new FakeRouteService());

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "",
            Destination = "Laval, QC",
            TravelMode = "car"
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task ComputeRoute_ReturnsBadRequest_WhenDestinationIsEmpty()
    {
        var controller = CreateController(new FakeRouteService());

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "Montreal, QC",
            Destination = " ",
            TravelMode = "bike"
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task ComputeRoute_ReturnsBadRequest_ForUnsupportedTravelMode()
    {
        var controller = CreateController(new FakeRouteService());

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "Montreal, QC",
            Destination = "Laval, QC",
            TravelMode = "walk"
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task ComputeRoute_Returns502_WhenGoogleRoutesExceptionThrown()
    {
        var service = new FakeRouteService
        {
            ExceptionToThrow = new GoogleRoutesException("Upstream failure", 503)
        };
        var controller = CreateController(service);

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "Montreal, QC",
            Destination = "Laval, QC",
            TravelMode = "car"
        });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, obj.StatusCode);
    }

    [Fact]
    public async Task ComputeRoute_Returns500_WhenUnexpectedExceptionThrown()
    {
        var service = new FakeRouteService
        {
            ExceptionToThrow = new InvalidOperationException("Something broke")
        };
        var controller = CreateController(service);

        var result = await controller.ComputeRoute(new RouteRequest
        {
            Origin = "Montreal, QC",
            Destination = "Laval, QC",
            TravelMode = "bike"
        });

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, obj.StatusCode);
    }

    private static MobilityController CreateController(IRouteService routeService)
    {
        return new MobilityController(
            Array.Empty<IMobilityProviderAdapter>(),
            new StubLocationService(),
            routeService,
            NullLogger<MobilityController>.Instance);
    }

    private sealed class FakeRouteService : IRouteService
    {
        public Exception? ExceptionToThrow { get; set; }

        public Task<RouteResult> ComputeRouteAsync(
            string origin,
            string destination,
            string travelMode,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            return Task.FromResult(new RouteResult(12500, "900s", "encodedPolylineData"));
        }
    }

    private sealed class StubLocationService : IMobilityLocationService
    {
        public Task<IEnumerable<MobilityLocation>> GetAllAsync(string? type = null)
            => Task.FromResult(Enumerable.Empty<MobilityLocation>());

        public Task<MobilityLocation?> GetByIdAsync(int id)
            => Task.FromResult<MobilityLocation?>(null);

        public Task<MobilityLocation> InsertAsync(
            string placeId, string name, string type, string city,
            double latitude, double longitude, int capacity, int availableSpots)
            => throw new NotImplementedException();

        public Task<MobilityLocation?> UpdateAvailableSpotsAsync(int id, int availableSpots)
            => throw new NotImplementedException();

        public Task<bool> DeleteAsync(int id)
            => throw new NotImplementedException();

        public Task<object> GetCityAnalyticsAsync()
            => throw new NotImplementedException();
    }
}
