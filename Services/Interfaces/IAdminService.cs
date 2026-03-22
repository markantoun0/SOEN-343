using YourProject.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IAdminService
{
    /// <summary>Creates a new admin account. Throws InvalidOperationException if email is taken.</summary>
    Task<Admin> RegisterAsync(string name, string email, string password);

    /// <summary>Returns the admin on success, null if credentials are wrong.</summary>
    Task<Admin?> LoginAsync(string email, string password);
}
