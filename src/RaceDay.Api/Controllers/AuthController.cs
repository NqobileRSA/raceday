using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout. A successful login stores the user ID and role in a server-side session.</summary>
[Route("api/auth")]
public class AuthController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;

    public AuthController(RaceDayDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    /// <summary>Register a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// The password is hashed (PBKDF2) before it is stored. On success the new user is logged in
    /// immediately: the response sets the RaceDay session cookie, so later requests are authenticated.
    /// </remarks>
    /// <response code="201">Account created and session started. Returns the user profile.</response>
    /// <response code="400">Validation failed (missing fields, invalid role or gender, weak password).</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var role = Lookup.Normalise(request.Role, Roles.All);
        if (role is null)
            return BadRequestMessage("Role must be either 'Organiser' or 'Participant'.");

        string? gender = null;
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            gender = Lookup.Normalise(request.Gender, Genders.All);
            if (gender is null) return BadRequestMessage("Gender must be 'Male', 'Female' or 'Other'.");
        }

        if (!IsStrongPassword(request.Password))
            return BadRequestMessage("Password must be at least 8 characters and contain at least one letter and one number.");

        if (request.DateOfBirth is { } dob && dob > Today)
            return BadRequestMessage("Date of birth cannot be in the future.");

        var email = NormaliseEmail(request.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return ConflictMessage("An account with this email address already exists.");

        var user = new User
        {
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = role,
            Phone = request.Phone?.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = gender,
            Province = request.Province?.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        user.PasswordHash = _passwords.Hash(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        StartSession(user);
        return StatusCode(StatusCodes.Status201Created,
            new AuthResponse { Message = "Registration successful. You are now logged in.", User = user.ToResponse() });
    }

    /// <summary>Log in with email and password.</summary>
    /// <remarks>
    /// On success the server stores the user ID and role in the session and returns the session cookie.
    /// Swagger UI sends the cookie automatically on later requests from the same browser.
    /// Seeded demo accounts all use the password Password123!
    /// </remarks>
    /// <response code="200">Logged in. Returns the user profile.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = NormaliseEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for unknown email and wrong password, so the API does not reveal which emails exist.
        if (user is null || !_passwords.Verify(user, request.Password))
            return Unauthorized(new MessageResponse("Invalid email or password."));

        StartSession(user);
        return Ok(new AuthResponse { Message = "Login successful.", User = user.ToResponse() });
    }

    /// <summary>End the current session.</summary>
    /// <remarks>Clears every value stored in the session. Safe to call when already logged out.</remarks>
    /// <response code="200">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("You have been logged out."));
    }

    private void StartSession(User user)
    {
        var session = HttpContext.Session;
        session.Clear();
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.Email, user.Email);
        session.SetString(SessionKeys.FullName, user.FullName);
    }

    private static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    internal static bool IsStrongPassword(string? password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= 8
        && Regex.IsMatch(password, "[A-Za-z]")
        && Regex.IsMatch(password, "[0-9]");
}
