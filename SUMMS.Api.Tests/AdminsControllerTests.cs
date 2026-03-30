using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Controllers;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.Services.Interfaces;
using YourProject.Models;

namespace SUMMS.Api.Tests;

public class AdminsControllerTests
{
    [Fact]
    public async Task SignUp_ReturnsCreated_ForValidRequest()
    {
        var controller = CreateController();

        var result = await controller.SignUp(new AdminRegisterRequest
        {
            Name = "Platform Admin",
            Email = "admin@summs.com",
            Password = "secret123"
        });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForInvalidCredentials()
    {
        var service = new FakeAdminService();
        await service.RegisterAsync("Platform Admin", "admin@summs.com", "secret123");
        var controller = new AdminsController(service, new StubLocationService());

        var result = await controller.Login(new AdminLoginRequest
        {
            Email = "admin@summs.com",
            Password = "wrong-password"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsOk_ForValidCredentials()
    {
        var service = new FakeAdminService();
        await service.RegisterAsync("Platform Admin", "admin@summs.com", "secret123");
        var controller = new AdminsController(service, new StubLocationService());

        var result = await controller.Login(new AdminLoginRequest
        {
            Email = "admin@summs.com",
            Password = "secret123"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    private static AdminsController CreateController()
    {
        return new AdminsController(new FakeAdminService(), new StubLocationService());
    }

    private sealed class FakeAdminService : IAdminService
    {
        private readonly List<Admin> _admins = [];
        private int _nextId = 1;

        public Task<Admin> RegisterAsync(string name, string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (_admins.Any(a => a.Email == normalizedEmail))
                throw new InvalidOperationException("An admin account with this email already exists.");

            var admin = new Admin
            {
                Id = _nextId++,
                Name = name.Trim(),
                Email = normalizedEmail,
                PasswordHash = SUMMS.Api.Services.PasswordHelper.Hash(password)
            };

            _admins.Add(admin);
            return Task.FromResult(admin);
        }

        public Task<Admin?> LoginAsync(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var admin = _admins.FirstOrDefault(a => a.Email == normalizedEmail);
            if (admin is null) return Task.FromResult<Admin?>(null);

            return Task.FromResult(
                SUMMS.Api.Services.PasswordHelper.Verify(password, admin.PasswordHash)
                    ? admin
                    : null);
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
