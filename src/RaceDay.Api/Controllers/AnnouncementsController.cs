using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Updates posted by the Organiser for an event (route changes, race pack collection, etc.).</summary>
[Route("api/events/{eventId:int}/announcements")]
public class AnnouncementsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public AnnouncementsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List the announcements for an event, newest first.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="200">Array of announcements.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<AnnouncementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFoundMessage($"Event {eventId} was not found.");

        var items = await _db.EventAnnouncements
            .AsNoTracking()
            .Include(a => a.Author)
            .Where(a => a.EventId == eventId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(a => a.ToResponse()).ToList());
    }

    /// <summary>Post an announcement for an event. Only the owning Organiser may do this.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">The announcement title, message and optional priority.</param>
    /// <response code="201">The new announcement.</response>
    /// <response code="400">Validation failed (missing title or message, invalid priority).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CreateAnnouncementRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFoundMessage($"Event {eventId} was not found.");
        var authorId = RequireUserId();
        if (ev.OrganiserId != authorId) return Forbidden("You can only post announcements for events that you created.");

        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequestMessage("title is required.");
        if (string.IsNullOrWhiteSpace(request.Message)) return BadRequestMessage("message is required.");

        var priority = AnnouncementPriorities.Normal;
        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var p = Lookup.Normalise(request.Priority, AnnouncementPriorities.All);
            if (p is null) return BadRequestMessage("priority must be Low, Normal, High or Urgent.");
            priority = p;
        }

        var announcement = new EventAnnouncement
        {
            EventId = eventId,
            AuthorId = authorId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Priority = priority,
            CreatedAt = DateTime.Now
        };

        _db.EventAnnouncements.Add(announcement);
        await _db.SaveChangesAsync();

        announcement.Author = await _db.Users.FindAsync(authorId);
        return StatusCode(StatusCodes.Status201Created, announcement.ToResponse());
    }

    /// <summary>Delete an announcement. Only the owning Organiser may do this.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="announcementId">The announcement ID.</param>
    /// <response code="200">Confirmation message.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event or announcement does not exist.</response>
    [HttpDelete("{announcementId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int eventId, int announcementId)
    {
        var announcement = await _db.EventAnnouncements
            .Include(a => a.Event)
            .FirstOrDefaultAsync(a => a.AnnouncementId == announcementId && a.EventId == eventId);

        if (announcement is null) return NotFoundMessage($"Announcement {announcementId} was not found for event {eventId}.");
        if (announcement.Event!.OrganiserId != RequireUserId()) return Forbidden("You can only delete announcements for events that you created.");

        _db.EventAnnouncements.Remove(announcement);
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse("Announcement deleted."));
    }
}
