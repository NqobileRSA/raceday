using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>A finish time, position and status captured by the Organiser after the event.</summary>
public class CreateResultRequest
{
    /// <summary>Finish time as hh:mm:ss. Required when status is Finished.</summary>
    /// <example>01:42:35</example>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position within the category. Required when status is Finished.</summary>
    /// <example>47</example>
    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    /// <summary>"Finished", "DNF", "DNS" or "DQ".</summary>
    /// <example>Finished</example>
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Result fields that may be corrected. Only the fields that are sent are updated.</summary>
public class UpdateResultRequest
{
    /// <example>01:41:59</example>
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    /// <summary>"Finished", "DNF", "DNS" or "DQ".</summary>
    public string? Status { get; set; }
}

/// <summary>A result with event, category and participant details.</summary>
public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
    public int EventId { get; set; }
    public string? EventTitle { get; set; }
    public DateOnly? EventDate { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? BibNumber { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>Number of Finished results in the same category, so the UI can show "47th out of 312".</summary>
    public int TotalFinishers { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

/// <summary>Leaderboard for one category of an event.</summary>
public class CategoryResultsResponse
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public List<ResultResponse> Results { get; set; } = new();
}
