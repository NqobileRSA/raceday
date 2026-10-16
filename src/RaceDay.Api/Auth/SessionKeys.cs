namespace RaceDay.Api.Auth;

/// <summary>Keys used to store the authenticated user's identity in the server-side session.</summary>
public static class SessionKeys
{
    public const string UserId = "RaceDay.UserId";
    public const string Role = "RaceDay.Role";
    public const string Email = "RaceDay.Email";
    public const string FullName = "RaceDay.FullName";
}
