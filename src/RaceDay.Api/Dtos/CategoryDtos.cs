using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Fields required to add an age or distance category to an event.</summary>
public class CreateCategoryRequest
{
    /// <example>Under 20 - 10km</example>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <example>10.00</example>
    [Required, Range(0.1, 999.99)]
    public decimal? DistanceKm { get; set; }

    /// <summary>Entry fee in Rand. Defaults to 0.00.</summary>
    /// <example>120.00</example>
    [Range(0, 99999999)]
    public decimal? EntryFee { get; set; }

    /// <example>500</example>
    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }

    /// <example>12</example>
    [Range(0, 120)]
    public int? MinAge { get; set; }

    /// <example>19</example>
    [Range(0, 120)]
    public int? MaxAge { get; set; }

    /// <summary>"Male", "Female" or empty for open categories.</summary>
    public string? GenderRestriction { get; set; }
}

/// <summary>Category fields that may be changed. Only the fields that are sent are updated.</summary>
public class UpdateCategoryRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [Range(0.1, 999.99)]
    public decimal? DistanceKm { get; set; }

    [Range(0, 99999999)]
    public decimal? EntryFee { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    public int? MaxAge { get; set; }

    /// <summary>"Male", "Female", or "None" to remove the restriction.</summary>
    public string? GenderRestriction { get; set; }
}

/// <summary>A category with its current enrolment count.</summary>
public class CategoryResponse
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

    /// <summary>Spots left before the category is full. Null when there is no limit.</summary>
    public int? SpotsRemaining { get; set; }
}
