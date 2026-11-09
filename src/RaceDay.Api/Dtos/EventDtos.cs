using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Fields required to create an event.</summary>
public class CreateEventRequest
{
    /// <example>Durban Beachfront 10k</example>
    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <example>A flat, fast 10km along the Golden Mile promenade.</example>
    public string? Description { get; set; }

    /// <example>2026-12-06</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <summary>Headline distance in km, e.g. 10 or 42.2.</summary>
    /// <example>10</example>
    [Range(0.1, 999.99)]
    public decimal? DistanceKm { get; set; }

    /// <example>uShaka Marine World, Durban</example>
    [Required, MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    /// <example>KwaZulu-Natal</example>
    [MaxLength(50)]
    public string? Province { get; set; }

    /// <summary>"Running", "Walking" or "Cycling".</summary>
    /// <example>Running</example>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>"Draft" (default), "Published", "Completed" or "Cancelled". Only Published events are listed publicly.</summary>
    /// <example>Published</example>
    public string? Status { get; set; }

    /// <example>info@durban10k.co.za</example>
    [EmailAddress, MaxLength(255)]
    public string? ContactEmail { get; set; }

    [Url, MaxLength(500)]
    public string? ImageUrl { get; set; }
}

/// <summary>Event fields that may be changed. Only the fields that are sent are updated.</summary>
public class UpdateEventRequest
{
    [MaxLength(255)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? EventDate { get; set; }

    [Range(0.1, 999.99)]
    public decimal? DistanceKm { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? Province { get; set; }

    /// <summary>"Running", "Walking" or "Cycling".</summary>
    public string? EventType { get; set; }

    /// <summary>"Draft", "Published", "Completed" or "Cancelled".</summary>
    public string? Status { get; set; }

    [EmailAddress, MaxLength(255)]
    public string? ContactEmail { get; set; }

    [Url, MaxLength(500)]
    public string? ImageUrl { get; set; }
}

/// <summary>Summary view of an event, used in lists.</summary>
public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string? OrganiserName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }

    /// <summary>Headline distance set by the Organiser, or null.</summary>
    public decimal? DistanceKm { get; set; }

    public string Location { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>The distinct distances offered by the event's categories, shortest first.</summary>
    public List<decimal> DistancesKm { get; set; } = new();

    public int CategoryCount { get; set; }

    /// <summary>Active (non-cancelled) enrolments across all categories.</summary>
    public int EnrolmentCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Full event view with categories, route and announcements.</summary>
public class EventDetailResponse : EventResponse
{
    public List<CategoryResponse> Categories { get; set; } = new();
    public RouteResponse? Route { get; set; }
    public List<AnnouncementResponse> Announcements { get; set; } = new();
}
