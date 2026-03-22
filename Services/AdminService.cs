using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Services.Interfaces;
using YourProject.Models;

namespace SUMMS.Api.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext db, ILogger<AdminService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Admin> RegisterAsync(string name, string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        _logger.LogInformation("Admin register attempt for email={Email}", normalizedEmail);

        var exists = await _db.Admins.AnyAsync(a => a.Email == normalizedEmail);
        if (exists)
            throw new InvalidOperationException("An admin account with this email already exists.");

        var admin = new Admin
        {
            Name = name.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHelper.Hash(password)
        };

        _db.Admins.Add(admin);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin created Id={Id}", admin.Id);
        return admin;
    }

    public async Task<Admin?> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        _logger.LogInformation("Admin login attempt for email={Email}", normalizedEmail);

        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == normalizedEmail);
        if (admin is null) return null;

        return PasswordHelper.Verify(password, admin.PasswordHash) ? admin : null;
    }
}
