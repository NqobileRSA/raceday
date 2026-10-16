using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Details needed to create a new RaceDay account.</summary>
public class RegisterRequest
{
    /// <example>naledi.khumalo@gmail.com</example>
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters with at least one letter and one number.</summary>
    /// <example>Password123!</example>
    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <example>Naledi</example>
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <example>Khumalo</example>
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <example>072 555 0404</example>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <example>1998-02-14</example>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>"Male", "Female" or "Other".</summary>
    /// <example>Female</example>
    [MaxLength(10)]
    public string? Gender { get; set; }

    /// <example>Gauteng</example>
    [MaxLength(50)]
    public string? Province { get; set; }
}

/// <summary>Credentials used to log in.</summary>
public class LoginRequest
{
    /// <example>thabo.mokoena@raceday.co.za</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Returned after a successful register or login. The session cookie is set on the same response.</summary>
public class AuthResponse
{
    public string Message { get; set; } = string.Empty;
    public UserResponse User { get; set; } = new();
}
