using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>The outcome of one enrolment: finish time, finishing position and status (Finished, DNF, DNS, DQ).</summary>
[Table("Results")]
public class Result
{
    [Key]
    [Column("result_id")]
    public int ResultId { get; set; }

    [Column("enrolment_id")]
    public int EnrolmentId { get; set; }

    [Column("finish_time", TypeName = "time")]
    public TimeSpan? FinishTime { get; set; }

    [Column("position")]
    public int? Position { get; set; }

    [Required, MaxLength(20)]
    [Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = ResultStatuses.Finished;

    [Column("recorded_by")]
    public int? RecordedById { get; set; }

    [Column("recorded_at", TypeName = "datetime")]
    public DateTime RecordedAt { get; set; } = DateTime.Now;

    public Enrolment? Enrolment { get; set; }
    public User? RecordedBy { get; set; }
}
