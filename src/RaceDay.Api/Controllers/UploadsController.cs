using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Image uploads stored in Azure Blob Storage. The MVC app posts files here; it never talks to Azure directly.
/// The blob URL is saved to the database and returned so the MVC app can display it.
/// </summary>
[Tags("Uploads")]
public class UploadsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IImageStorage _storage;

    public UploadsController(RaceDayDbContext db, IImageStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Upload or replace an event's banner image. Only the owning Organiser.</summary>
    /// <remarks>multipart/form-data with a single field named <c>file</c>. JPG, PNG or WebP, up to 5 MB.</remarks>
    /// <param name="id">The event ID.</param>
    /// <param name="request">The image file.</param>
    /// <param name="cancellationToken">Cancelled if the client disconnects.</param>
    /// <response code="200">The updated event with its new image_url.</response>
    /// <response code="400">No file, wrong type, too large, or not a real image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="503">Azure Blob Storage could not be reached.</response>
    [HttpPost("api/events/{id:int}/banner")]
    [SessionAuthorize(Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageRules.MaxBytes + 64 * 1024)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadEventBanner(int id, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id, cancellationToken);
        if (ev is null) return NotFoundMessage($"Event {id} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only change the banner of events that you created.");

        var error = ImageRules.Validate(request.File);
        if (error is not null) return BadRequestMessage(error);

        try
        {
            await using var stream = request.File!.OpenReadStream();
            var url = await _storage.UploadAsync(stream, request.File.ContentType, ImageContainers.EventBanners, cancellationToken);
            var old = ev.ImageUrl;
            ev.ImageUrl = url;
            ev.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            await _storage.DeleteAsync(old, cancellationToken);
            return Ok(ev.ToResponse());
        }
        catch (ImageStorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse(ex.Message));
        }
    }

    /// <summary>Upload or replace the logged-in user's profile picture.</summary>
    /// <remarks>multipart/form-data with a single field named <c>file</c>. JPG, PNG or WebP, up to 5 MB.</remarks>
    /// <param name="request">The image file.</param>
    /// <param name="cancellationToken">Cancelled if the client disconnects.</param>
    /// <response code="200">The updated profile with its new profile_picture_url.</response>
    /// <response code="400">No file, wrong type, too large, or not a real image.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Azure Blob Storage could not be reached.</response>
    [HttpPost("api/users/me/profile-picture")]
    [SessionAuthorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageRules.MaxBytes + 64 * 1024)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(RequireUserId());
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new MessageResponse("Your session is no longer valid. Please log in again."));
        }

        var error = ImageRules.Validate(request.File);
        if (error is not null) return BadRequestMessage(error);

        try
        {
            await using var stream = request.File!.OpenReadStream();
            var url = await _storage.UploadAsync(stream, request.File.ContentType, ImageContainers.ProfilePictures, cancellationToken);
            var old = user.ProfilePictureUrl;
            user.ProfilePictureUrl = url;
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            await _storage.DeleteAsync(old, cancellationToken);
            return Ok(user.ToResponse());
        }
        catch (ImageStorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse(ex.Message));
        }
    }
}
