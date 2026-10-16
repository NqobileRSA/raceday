using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Route information for an event (start and end coordinates, elevation, terrain). One route per event.</summary>
[Route("api/events/{eventId:int}/route")]
public class RoutesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public RoutesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Get the route for an event.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="200">The route.</response>
    /// <response code="404">The event does not exist or has no route yet.</response>
    [HttpGet]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFoundMessage($"Event {eventId} was not found.");

        var route = await _db.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.EventId == eventId);
        return route is null
            ? NotFoundMessage($"Event {eventId} does not have a route yet.")
            : Ok(route.ToResponse());
    }

    /// <summary>Add a route to an event. Only the owning Organiser may do this.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">Route coordinates, description, elevation and terrain.</param>
    /// <response code="201">The new route.</response>
    /// <response code="400">Validation failed (coordinates out of range).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">The event already has a route. Use PUT to change it.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, [FromBody] RouteRequest request)
    {
        var ev = await _db.Events.Include(e => e.Route).FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFoundMessage($"Event {eventId} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only add a route to events that you created.");
        if (ev.Route is not null) return ConflictMessage("This event already has a route. Use PUT to update it.");

        var route = new EventRoute { EventId = eventId };
        Apply(route, request);

        _db.Routes.Add(route);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, route.ToResponse());
    }

    /// <summary>Update the route for an event. Only the owning Organiser may do this.</summary>
    /// <remarks>Only the fields included in the body are changed.</remarks>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">Route coordinates, description, elevation and terrain.</param>
    /// <response code="200">The updated route.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist or has no route yet.</response>
    [HttpPut]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int eventId, [FromBody] RouteRequest request)
    {
        var ev = await _db.Events.Include(e => e.Route).FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFoundMessage($"Event {eventId} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only update routes for events that you created.");
        if (ev.Route is null) return NotFoundMessage($"Event {eventId} does not have a route yet. Use POST to add one.");

        Apply(ev.Route, request);
        await _db.SaveChangesAsync();
        return Ok(ev.Route.ToResponse());
    }

    private static void Apply(EventRoute route, RouteRequest r)
    {
        if (r.StartLatitude is not null) route.StartLatitude = r.StartLatitude;
        if (r.StartLongitude is not null) route.StartLongitude = r.StartLongitude;
        if (r.EndLatitude is not null) route.EndLatitude = r.EndLatitude;
        if (r.EndLongitude is not null) route.EndLongitude = r.EndLongitude;
        if (r.Description is not null) route.Description = r.Description;
        if (r.ElevationGainM is not null) route.ElevationGainM = r.ElevationGainM;
        if (r.TerrainType is not null) route.TerrainType = r.TerrainType.Trim();
    }
}
