using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Links a Participant to a Category. Because a category belongs to exactly one event,
/// this single row records the Participant, the event and the selected category together.
/// </summary>
[Table("Enrolments")]
public class Enrolment
{
    [Key]
    [Column("enrolment_id")]
    public int EnrolmentId { get; set; }

    [Column("participant_id")]
    public int ParticipantId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("enrolment_date", TypeName = "datetime")]
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;

    [MaxLength(20)]
    [Column("bib_number", TypeName = "varchar(20)")]
    public string? BibNumber { get; set; }

    [Required, MaxLength(20)]
    [Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = EnrolmentStatuses.Pending;

    [MaxLength(100)]
    [Column("emergency_contact_name", TypeName = "varchar(100)")]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    [Column("emergency_contact_phone", TypeName = "varchar(20)")]
    public string? EmergencyContactPhone { get; set; }

    public User? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}
