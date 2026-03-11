using YourProject.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IUserService
{
    /// <summary>Creates a new user. Throws InvalidOperationException if email is taken.</summary>
    Task<User> SignUpAsync(string name, string email, string password);

    /// <summary>Returns the user on success, null if credentials are wrong.</summary>
    Task<User?> LoginAsync(string email, string password);
}
