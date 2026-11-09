using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>A road event (run, walk or cycle) owned by one Organiser.</summary>
[Table("Events")]
public class Event
{
    [Key]
    [Column("event_id")]
    public int EventId { get; set; }

    [Column("organiser_id")]
    public int OrganiserId { get; set; }

    [Required, MaxLength(255)]
    [Column("title", TypeName = "varchar(255)")]
    public string Title { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    /// <summary>Headline distance shown on the event card (added in Part 3). Categories can still offer other distances.</summary>
    [Column("distance_km", TypeName = "decimal(5,2)")]
    public decimal? DistanceKm { get; set; }

    [Column("event_date", TypeName = "date")]
    public DateOnly EventDate { get; set; }

    [Required, MaxLength(255)]
    [Column("location", TypeName = "varchar(255)")]
    public string Location { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("province", TypeName = "varchar(50)")]
    public string? Province { get; set; }

    [Required, MaxLength(20)]
    [Column("event_type", TypeName = "varchar(20)")]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = EventStatuses.Draft;

    [MaxLength(255)]
    [Column("contact_email", TypeName = "varchar(255)")]
    public string? ContactEmail { get; set; }

    /// <summary>Azure Blob Storage URL of the event banner.</summary>
    [MaxLength(500)]
    [Column("image_url", TypeName = "varchar(500)")]
    public string? ImageUrl { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public EventRoute? Route { get; set; }
    public ICollection<EventAnnouncement> Announcements { get; set; } = new List<EventAnnouncement>();
}
