using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Route details for an event. Used for both create and update.</summary>
public class RouteRequest
{
    /// <example>-26.2359</example>
    [Range(-90, 90)]
    public decimal? StartLatitude { get; set; }

    /// <example>27.9825</example>
    [Range(-180, 180)]
    public decimal? StartLongitude { get; set; }

    [Range(-90, 90)]
    public decimal? EndLatitude { get; set; }

    [Range(-180, 180)]
    public decimal? EndLongitude { get; set; }

    public string? Description { get; set; }

    /// <example>185</example>
    [Range(0, 10000)]
    public int? ElevationGainM { get; set; }

    /// <example>Road</example>
    [MaxLength(50)]
    public string? TerrainType { get; set; }
}

public class RouteResponse
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public string? Description { get; set; }
    public int? ElevationGainM { get; set; }
    public string? TerrainType { get; set; }
}

/// <summary>A new announcement for an event.</summary>
public class CreateAnnouncementRequest
{
    /// <example>Race Pack Collection</example>
    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <example>Race packs can be collected on Friday between 09:00 and 17:00.</example>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>"Low", "Normal" (default), "High" or "Urgent".</summary>
    /// <example>Normal</example>
    public string? Priority { get; set; }
}

public class AnnouncementResponse
{
    public int AnnouncementId { get; set; }
    public int EventId { get; set; }
    public int AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Live weather at the route start point.</summary>
public class WeatherResponse
{
    public int EventId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? ObservedAt { get; set; }
    public decimal TemperatureC { get; set; }
    public int HumidityPercent { get; set; }
    public decimal WindSpeedKmh { get; set; }
    public int WeatherCode { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
