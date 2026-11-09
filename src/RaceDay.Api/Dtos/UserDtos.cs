using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>A user's profile. The password hash is never included.</summary>
public class UserResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Province { get; set; }

    /// <summary>Azure Blob Storage URL of the profile picture, or null.</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Profile fields the user may change. Only the fields that are sent are updated.</summary>
public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    /// <summary>"Male", "Female" or "Other".</summary>
    [MaxLength(10)]
    public string? Gender { get; set; }

    [MaxLength(50)]
    public string? Province { get; set; }
}
