using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Optional details captured when a Participant enters an event.</summary>
public class EnrolRequest
{
    /// <example>Nomsa Ndlovu</example>
    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }

    /// <example>073 111 2233</example>
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }
}

/// <summary>Used by the Organiser to confirm an enrolment.</summary>
public class ConfirmEnrolmentRequest
{
    /// <example>JBM-2145</example>
    [MaxLength(20)]
    public string? BibNumber { get; set; }
}

/// <summary>An enrolment with the linked participant, event and category.</summary>
public class EnrolmentResponse
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

    /// <summary>"Pending", "Confirmed" or "Cancelled".</summary>
    public string Status { get; set; } = string.Empty;

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool HasResult { get; set; }
}
