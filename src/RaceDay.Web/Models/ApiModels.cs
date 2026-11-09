namespace RaceDay.Web.Models;

// Response shapes returned by the RaceDay API. JSON is snake_case; ApiClient maps it onto these PascalCase properties.

public class MessageDto
{
    public string Message { get; set; } = string.Empty;
}

public class UserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Province { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuthResponseDto
{
    public string Message { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string? OrganiserName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public decimal? DistanceKm { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ImageUrl { get; set; }
    public List<decimal> DistancesKm { get; set; } = new();
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }

    /// <summary>The headline distance, or the category distances when no headline distance is set.</summary>
    public string DistanceLabel => DistanceKm is { } d
        ? $"{d:0.#}km"
        : DistancesKm.Count == 0 ? "Distances to be announced" : string.Join(" / ", DistancesKm.Select(x => $"{x:0.#}km"));

    public bool IsUpcoming => EventDate >= DateOnly.FromDateTime(DateTime.Today);
}

public class EventDetailDto : EventDto
{
    public List<CategoryDto> Categories { get; set; } = new();
    public RouteDto? Route { get; set; }
    public List<AnnouncementDto> Announcements { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? GenderRestriction { get; set; }
    public int EnrolmentCount { get; set; }
    public int? SpotsRemaining { get; set; }

    public string AgeLabel => (MinAge, MaxAge) switch
    {
        (null, null) => "All ages",
        ({ } min, null) => $"{min}+",
        (null, { } max) => $"Up to {max}",
        ({ } min, { } max) => $"{min} to {max}"
    };

    public bool IsFull => SpotsRemaining is 0;
}

public class RouteDto
{
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public string? Description { get; set; }
    public int? ElevationGainM { get; set; }
    public string? TerrainType { get; set; }
}

public class AnnouncementDto
{
    public int AnnouncementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
    public string? ParticipantEmail { get; set; }
    public int EventId { get; set; }
    public string? EventTitle { get; set; }
    public DateOnly? EventDate { get; set; }
    public string? EventLocation { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? DistanceKm { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string? BibNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool HasResult { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string? ParticipantName { get; set; }
    public int EventId { get; set; }
    public string? EventTitle { get; set; }
    public DateOnly? EventDate { get; set; }
    public string? CategoryName { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? BibNumber { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CategoryResultsDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public List<ResultDto> Results { get; set; } = new();
}

public class WeatherDto
{
    public decimal TemperatureC { get; set; }
    public int HumidityPercent { get; set; }
    public decimal WindSpeedKmh { get; set; }
    public string Conditions { get; set; } = string.Empty;
}
