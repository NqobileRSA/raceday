using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Live race-day weather at the route start point, from the Open-Meteo API.</summary>
public class WeatherController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IWeatherService _weather;

    public WeatherController(RaceDayDbContext db, IWeatherService weather)
    {
        _db = db;
        _weather = weather;
    }

    /// <summary>Get current weather for an event's route start location. Any logged-in user.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="cancellationToken">Cancelled if the client disconnects.</param>
    /// <response code="200">Temperature, conditions, wind and humidity.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The event does not exist, or its route has no start coordinates.</response>
    /// <response code="503">The external weather API could not be reached.</response>
    [HttpGet("api/events/{eventId:int}/weather")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(int eventId, CancellationToken cancellationToken)
    {
        var ev = await _db.Events.AsNoTracking().Include(e => e.Route).FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null) return NotFoundMessage($"Event {eventId} was not found.");
        if (ev.Route?.StartLatitude is null || ev.Route.StartLongitude is null)
            return NotFoundMessage("This event does not have route start coordinates, so weather cannot be looked up.");

        try
        {
            var weather = await _weather.GetCurrentAsync(ev.Route.StartLatitude.Value, ev.Route.StartLongitude.Value, cancellationToken);
            weather.EventId = eventId;
            return Ok(weather);
        }
        catch (WeatherUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new MessageResponse(ex.Message));
        }
    }
}
