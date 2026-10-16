namespace RaceDay.Api.Models;

/// <summary>
/// Allowed values for every column that has a CHECK constraint in the Part 1 SQL script.
/// Keeping them in one place means the API validation and the database constraints never drift apart.
/// </summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
    public static readonly string[] All = { Organiser, Participant };
}

public static class Genders
{
    public static readonly string[] All = { "Male", "Female", "Other" };
    public static readonly string[] Restrictions = { "Male", "Female" };
}

public static class EventTypes
{
    public static readonly string[] All = { "Running", "Walking", "Cycling" };
}

public static class EventStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = { Draft, Published, Completed, Cancelled };
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = { Pending, Confirmed, Cancelled };
}

public static class ResultStatuses
{
    public const string Finished = "Finished";
    public static readonly string[] All = { Finished, "DNF", "DNS", "DQ" };
}

public static class AnnouncementPriorities
{
    public const string Normal = "Normal";
    public static readonly string[] All = { "Low", Normal, "High", "Urgent" };
}

public static class Lookup
{
    /// <summary>
    /// Matches a value against an allowed list, ignoring case, and returns the canonical spelling
    /// (so "running" is stored as "Running" and passes the database CHECK constraint).
    /// </summary>
    public static string? Normalise(string? value, IEnumerable<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
