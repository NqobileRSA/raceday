using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>A registered account. The role column decides whether the user is an Organiser or a Participant.</summary>
[Table("Users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required, MaxLength(255)]
    [Column("email", TypeName = "varchar(255)")]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Column("password_hash", TypeName = "varchar(255)")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Column("first_name", TypeName = "varchar(100)")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Column("last_name", TypeName = "varchar(100)")]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Column("role", TypeName = "varchar(20)")]
    public string Role { get; set; } = Roles.Participant;

    [MaxLength(20)]
    [Column("phone", TypeName = "varchar(20)")]
    public string? Phone { get; set; }

    [Column("date_of_birth", TypeName = "date")]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(10)]
    [Column("gender", TypeName = "varchar(10)")]
    public string? Gender { get; set; }

    [MaxLength(50)]
    [Column("province", TypeName = "varchar(50)")]
    public string? Province { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<EventAnnouncement> Announcements { get; set; } = new List<EventAnnouncement>();
    public ICollection<Result> RecordedResults { get; set; } = new List<Result>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
