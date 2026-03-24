using YourProject.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IUserService
{
    /// <summary>Creates a new user. Throws InvalidOperationException if email is taken.</summary>
    Task<User> SignUpAsync(string name, string email, string password);

    /// <summary>Returns the user on success, null if credentials are wrong.</summary>
    Task<User?> LoginAsync(string email, string password);

    /// <summary>Returns a user by id, or null if not found.</summary>
    Task<User?> GetByIdAsync(int userId);

    /// <summary>Updates user preferences. Returns false if user does not exist.</summary>
    Task<bool> UpdatePreferencesAsync(int userId, string preferredCity, string preferredMobilityType);
}
