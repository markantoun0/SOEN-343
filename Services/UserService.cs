using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Services.Interfaces;
using YourProject.Models;

namespace SUMMS.Api.Services;

public class UserService : IUserService
{
    private static readonly HashSet<string> AllowedCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "montreal",
        "laval"
    };

    private static readonly HashSet<string> AllowedMobilityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bixi",
        "parking"
    };

    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<User> SignUpAsync(string name, string email, string password)
    {
        _logger.LogInformation("SignUp attempt for email={Email}", email);

        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Name         = name.Trim(),
            Email        = email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHelper.Hash(password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User created Id={Id}", user.Id);
        return user;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        _logger.LogInformation("Login attempt for email={Email}", email);

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == email.Trim().ToLowerInvariant());

        if (user is null) return null;
        if (!PasswordHelper.Verify(password, user.PasswordHash)) return null;

        return user;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> UpdatePreferencesAsync(int userId, string preferredCity, string preferredMobilityType)
    {
        var normalizedCity = NormalizePreferredCity(preferredCity);
        var normalizedMobilityType = NormalizePreferredMobilityType(preferredMobilityType);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return false;

        user.PreferredCity = normalizedCity;
        user.PreferredMobilityType = normalizedMobilityType;

        await _db.SaveChangesAsync();
        return true;
    }

    private static string NormalizePreferredCity(string preferredCity)
    {
        var city = preferredCity?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(city) || !AllowedCities.Contains(city))
        {
            throw new ArgumentException("Preferred city must be either montreal or laval.");
        }

        return city;
    }

    private static string NormalizePreferredMobilityType(string preferredMobilityType)
    {
        var mobilityType = preferredMobilityType?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(mobilityType) || !AllowedMobilityTypes.Contains(mobilityType))
        {
            throw new ArgumentException("Preferred mobility type must be either bixi or parking.");
        }

        return mobilityType;
    }
}
