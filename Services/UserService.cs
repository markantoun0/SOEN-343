using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Services.Interfaces;
using YourProject.Models;

namespace SUMMS.Api.Services;

public class UserService : IUserService
{
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
}
