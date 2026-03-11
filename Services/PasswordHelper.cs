using System.Security.Cryptography;
using System.Text;

namespace SUMMS.Api.Services;

public static class PasswordHelper
{
    public static string Hash(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToBase64String(saltBytes);
        using var sha256 = SHA256.Create();
        var hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
        return $"{salt}:{hash}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':', 2);
        if (parts.Length != 2) return false;
        var salt         = parts[0];
        var expectedHash = parts[1];
        using var sha256 = SHA256.Create();
        var actualHash   = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password)));
        return actualHash == expectedHash;
    }
}
