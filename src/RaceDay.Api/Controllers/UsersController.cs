using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile. Available to both roles.</summary>
[Route("api/users")]
[SessionAuthorize]
public class UsersController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Get the profile of the logged-in user.</summary>
    /// <response code="200">The user profile (password hash is never returned).</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.FindAsync(RequireUserId());
        if (user is null)
        {
            // The account was removed after login; the session is stale.
            HttpContext.Session.Clear();
            return Unauthorized(new MessageResponse("Your session is no longer valid. Please log in again."));
        }

        return Ok(user.ToResponse());
    }

    /// <summary>Update the logged-in user's profile.</summary>
    /// <remarks>Only the fields included in the body are changed. Email and role cannot be changed here.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(RequireUserId());
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new MessageResponse("Your session is no longer valid. Please log in again."));
        }

        if (request.FirstName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName)) return BadRequestMessage("First name cannot be empty.");
            user.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.LastName)) return BadRequestMessage("Last name cannot be empty.");
            user.LastName = request.LastName.Trim();
        }

        if (request.Gender is not null)
        {
            var gender = Lookup.Normalise(request.Gender, Genders.All);
            if (gender is null) return BadRequestMessage("Gender must be 'Male', 'Female' or 'Other'.");
            user.Gender = gender;
        }

        if (request.DateOfBirth is { } dob)
        {
            if (dob > Today) return BadRequestMessage("Date of birth cannot be in the future.");
            user.DateOfBirth = dob;
        }

        if (request.Phone is not null) user.Phone = request.Phone.Trim();
        if (request.Province is not null) user.Province = request.Province.Trim();

        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
        return Ok(user.ToResponse());
    }
}
