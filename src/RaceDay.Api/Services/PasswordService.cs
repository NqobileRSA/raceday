using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Hashes and verifies passwords. Plain-text passwords are never stored.</summary>
public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}

/// <summary>
/// Uses the ASP.NET Core Identity PasswordHasher: PBKDF2 with HMAC-SHA512, a random 128-bit salt
/// and 100,000 iterations. The salt and iteration count are stored inside the hash string itself.
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(password)) return false;

        try
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // A hash that is not in the Identity format (for example an old placeholder) can never match.
            return false;
        }
    }
}
