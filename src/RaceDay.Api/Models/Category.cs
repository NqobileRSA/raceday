using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>A distance or age category inside an event, e.g. "21.1km Half Marathon" or "Under 20 - 10km".</summary>
[Table("Categories")]
public class Category
{
    [Key]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    [Column("name", TypeName = "varchar(100)")]
    public string Name { get; set; } = string.Empty;

    [Column("distance_km", TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }

    [Column("entry_fee", TypeName = "decimal(10,2)")]
    public decimal EntryFee { get; set; }

    [Column("max_participants")]
    public int? MaxParticipants { get; set; }

    [Column("min_age")]
    public int? MinAge { get; set; }

    [Column("max_age")]
    public int? MaxAge { get; set; }

    [MaxLength(10)]
    [Column("gender_restriction", TypeName = "varchar(10)")]
    public string? GenderRestriction { get; set; }

    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
