﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SUMMS.Api.Data;
using SUMMS.Api.Services;

namespace SUMMS.Api.Tests;

public class AdminServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesAdmin_WithHashedPassword()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var admin = await service.RegisterAsync("Platform Admin", "admin@summs.com", "secret123");

        Assert.NotEqual(0, admin.Id);
        Assert.Equal("Platform Admin", admin.Name);
        Assert.Equal("admin@summs.com", admin.Email);
        Assert.NotEqual("secret123", admin.PasswordHash);
        Assert.True(PasswordHelper.Verify("secret123", admin.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenEmailAlreadyExists()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.RegisterAsync("Admin One", "admin@summs.com", "secret123");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync("Admin Two", "ADMIN@summs.com", "another-secret"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_ForInvalidCredentials()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.RegisterAsync("Admin", "admin@summs.com", "secret123");

        var wrongPassword = await service.LoginAsync("admin@summs.com", "bad-password");
        var wrongEmail = await service.LoginAsync("missing@summs.com", "secret123");

        Assert.Null(wrongPassword);
        Assert.Null(wrongEmail);
    }

    [Fact]
    public async Task LoginAsync_ReturnsAdmin_ForValidCredentials()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.RegisterAsync("Admin", "admin@summs.com", "secret123");

        var admin = await service.LoginAsync("ADMIN@summs.com", "secret123");

        Assert.NotNull(admin);
        Assert.Equal("admin@summs.com", admin!.Email);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AdminService CreateService(AppDbContext db)
    {
        return new AdminService(db, NullLogger<AdminService>.Instance);
    }
}
