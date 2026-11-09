using RaceDay.Web.Models;

namespace RaceDay.Web.Infrastructure;

public static class SessionKeys
{
    public const string ApiCookie = "Api.Cookie";
    public const string UserId = "User.Id";
    public const string Role = "User.Role";
    public const string FirstName = "User.FirstName";
    public const string FullName = "User.FullName";
    public const string Email = "User.Email";
    public const string Avatar = "User.Avatar";
}

public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>The logged-in user as held in the MVC session. Null values mean "not logged in".</summary>
public record SessionUser(int Id, string Role, string FirstName, string FullName, string Email, string? Avatar)
{
    public bool IsOrganiser => Role == AppRoles.Organiser;
    public bool IsParticipant => Role == AppRoles.Participant;
    public string Initials => string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0])).ToUpperInvariant();
}

public static class SessionUserExtensions
{
    public static SessionUser? CurrentUser(this ISession session)
    {
        var id = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);
        if (id is null || string.IsNullOrEmpty(role)) return null;
        return new SessionUser(id.Value, role,
            session.GetString(SessionKeys.FirstName) ?? "",
            session.GetString(SessionKeys.FullName) ?? "",
            session.GetString(SessionKeys.Email) ?? "",
            session.GetString(SessionKeys.Avatar));
    }

    /// <summary>Stores the user's ID and role in the session after a successful API login or registration.</summary>
    public static void SignIn(this ISession session, UserDto user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        UpdateProfile(session, user);
    }

    public static void UpdateProfile(this ISession session, UserDto user)
    {
        session.SetString(SessionKeys.FirstName, user.FirstName);
        session.SetString(SessionKeys.FullName, $"{user.FirstName} {user.LastName}".Trim());
        session.SetString(SessionKeys.Email, user.Email);
        if (string.IsNullOrEmpty(user.ProfilePictureUrl)) session.Remove(SessionKeys.Avatar);
        else session.SetString(SessionKeys.Avatar, user.ProfilePictureUrl);
    }
}
