using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>An update posted by the owning Organiser, e.g. a route change or race pack collection notice.</summary>
[Table("EventAnnouncements")]
public class EventAnnouncement
{
    [Key]
    [Column("announcement_id")]
    public int AnnouncementId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("author_id")]
    public int AuthorId { get; set; }

    [Required, MaxLength(255)]
    [Column("title", TypeName = "varchar(255)")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message", TypeName = "text")]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Column("priority", TypeName = "varchar(20)")]
    public string Priority { get; set; } = AnnouncementPriorities.Normal;

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Event? Event { get; set; }
    public User? Author { get; set; }
}
