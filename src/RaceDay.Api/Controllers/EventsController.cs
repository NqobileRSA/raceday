using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (public) and manage them (owning Organiser only).</summary>
[Route("api/events")]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List published events, with optional filters.</summary>
    /// <remarks>
    /// Public. Results are ordered by event date. The total number of matching events is returned in
    /// the X-Total-Count response header so a front end can build paging controls.
    /// </remarks>
    /// <param name="province">Exact province name, e.g. Gauteng.</param>
    /// <param name="eventType">Running, Walking or Cycling.</param>
    /// <param name="fromDate">Only events on or after this date (yyyy-MM-dd).</param>
    /// <param name="toDate">Only events on or before this date (yyyy-MM-dd).</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="limit">Events per page (1 to 100).</param>
    /// <response code="200">Array of events with category and enrolment counts.</response>
    /// <response code="400">An invalid event_type or date range was supplied.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? province,
        [FromQuery(Name = "event_type")] string? eventType,
        [FromQuery(Name = "from_date")] DateOnly? fromDate,
        [FromQuery(Name = "to_date")] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        if (fromDate is not null && toDate is not null && fromDate > toDate)
            return BadRequestMessage("from_date must be on or before to_date.");

        var query = _db.Events
            .AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .Where(e => e.Status == EventStatuses.Published);

        if (!string.IsNullOrWhiteSpace(province))
        {
            var p = province.Trim().ToLower();
            query = query.Where(e => e.Province != null && e.Province.ToLower() == p);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = Lookup.Normalise(eventType, EventTypes.All);
            if (type is null) return BadRequestMessage("event_type must be Running, Walking or Cycling.");
            query = query.Where(e => e.EventType == type);
        }

        if (fromDate is not null) query = query.Where(e => e.EventDate >= fromDate);
        if (toDate is not null) query = query.Where(e => e.EventDate <= toDate);

        var total = await query.CountAsync();
        var (skip, take) = Paging(page, limit);
        var events = await query.OrderBy(e => e.EventDate).ThenBy(e => e.EventId).Skip(skip).Take(take).ToListAsync();

        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(events.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>Get full details of one event, including categories, route and announcements.</summary>
    /// <remarks>Public for Published, Completed and Cancelled events. Draft events are only visible to their Organiser.</remarks>
    /// <param name="id">The event ID.</param>
    /// <response code="200">The event with nested categories, route and announcements.</response>
    /// <response code="404">The event does not exist (or is a draft you do not own).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .Include(e => e.Route)
            .Include(e => e.Announcements).ThenInclude(a => a.Author)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null || (ev.Status == EventStatuses.Draft && ev.OrganiserId != CurrentUserId))
            return NotFoundMessage($"Event {id} was not found.");

        return Ok(ev.ToDetailResponse());
    }

    /// <summary>List the events created by the logged-in Organiser (all statuses, including drafts).</summary>
    /// <param name="status">Optional status filter: Draft, Published, Completed or Cancelled.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="limit">Events per page (1 to 100).</param>
    /// <response code="200">Array of the Organiser's events, newest event date first.</response>
    /// <response code="400">Invalid status filter.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpGet("my")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var organiserId = RequireUserId();
        var query = _db.Events
            .AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .Where(e => e.OrganiserId == organiserId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = Lookup.Normalise(status, EventStatuses.All);
            if (s is null) return BadRequestMessage("status must be Draft, Published, Completed or Cancelled.");
            query = query.Where(e => e.Status == s);
        }

        var total = await query.CountAsync();
        var (skip, take) = Paging(page, limit);
        var events = await query.OrderByDescending(e => e.EventDate).Skip(skip).Take(take).ToListAsync();

        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(events.Select(e => e.ToResponse()).ToList());
    }

    /// <summary>Create a new event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// The event's distances come from the categories added to it (POST /api/events/{eventId}/categories),
    /// because one event usually offers several distances (e.g. 42.2km, 21.1km and 10km).
    /// </remarks>
    /// <response code="201">The new event.</response>
    /// <response code="400">Validation failed (missing fields, invalid event type or status, date in the past).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var eventType = Lookup.Normalise(request.EventType, EventTypes.All);
        if (eventType is null) return BadRequestMessage("event_type must be Running, Walking or Cycling.");

        var status = EventStatuses.Draft;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var requested = Lookup.Normalise(request.Status, EventStatuses.All);
            if (requested is null) return BadRequestMessage("status must be Draft, Published, Completed or Cancelled.");
            status = requested;
        }

        if (request.EventDate is null) return BadRequestMessage("event_date is required.");
        if (request.EventDate < Today) return BadRequestMessage("event_date cannot be in the past.");
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequestMessage("title is required.");
        if (string.IsNullOrWhiteSpace(request.Location)) return BadRequestMessage("location is required.");

        var ev = new Event
        {
            OrganiserId = RequireUserId(),
            Title = request.Title.Trim(),
            Description = request.Description,
            EventDate = request.EventDate.Value,
            DistanceKm = request.DistanceKm,
            Location = request.Location.Trim(),
            Province = request.Province?.Trim(),
            EventType = eventType,
            Status = status,
            ContactEmail = request.ContactEmail?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        ev.Organiser = await _db.Users.FindAsync(ev.OrganiserId);
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ev.ToResponse());
    }

    /// <summary>Update an event. Only the Organiser who created it may do this.</summary>
    /// <remarks>Only the fields included in the body are changed.</remarks>
    /// <param name="id">The event ID.</param>
    /// <param name="request">The event fields to change.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null) return NotFoundMessage($"Event {id} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only update events that you created.");

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequestMessage("title cannot be empty.");
            ev.Title = request.Title.Trim();
        }

        if (request.Location is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Location)) return BadRequestMessage("location cannot be empty.");
            ev.Location = request.Location.Trim();
        }

        if (request.EventType is not null)
        {
            var type = Lookup.Normalise(request.EventType, EventTypes.All);
            if (type is null) return BadRequestMessage("event_type must be Running, Walking or Cycling.");
            ev.EventType = type;
        }

        if (request.Status is not null)
        {
            var status = Lookup.Normalise(request.Status, EventStatuses.All);
            if (status is null) return BadRequestMessage("status must be Draft, Published, Completed or Cancelled.");
            ev.Status = status;
        }

        if (request.EventDate is not null) ev.EventDate = request.EventDate.Value;
        if (request.DistanceKm is not null) ev.DistanceKm = request.DistanceKm;
        if (request.Description is not null) ev.Description = request.Description;
        if (request.Province is not null) ev.Province = request.Province.Trim();
        if (request.ContactEmail is not null) ev.ContactEmail = request.ContactEmail.Trim();
        if (request.ImageUrl is not null) ev.ImageUrl = request.ImageUrl.Trim();

        ev.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(ev.ToResponse());
    }

    /// <summary>Delete an event and everything linked to it. Only the owning Organiser may do this.</summary>
    /// <remarks>Categories, enrolments, results, the route and announcements are removed by cascading deletes.</remarks>
    /// <param name="id">The event ID.</param>
    /// <response code="200">Confirmation message.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFoundMessage($"Event {id} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only delete events that you created.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse($"Event '{ev.Title}' and all related data were deleted."));
    }
}
