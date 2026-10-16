using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Organisers capture finish times and positions after an event; Participants view their own race history.</summary>
public class ResultsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Record the result for one enrolment. Only the Organiser who owns the event may do this.</summary>
    /// <remarks>
    /// A Finished result needs both finish_time and position, and the position must not already be used in
    /// the same category. DNF, DNS and DQ results may leave them empty.
    /// </remarks>
    /// <param name="enrolmentId">The enrolment the result belongs to.</param>
    /// <param name="request">Status plus finish time and position.</param>
    /// <response code="201">The new result.</response>
    /// <response code="400">Validation failed (invalid status, missing time or position, duplicate position, cancelled enrolment).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The enrolment does not exist.</response>
    /// <response code="409">A result already exists for this enrolment. Use PUT /api/results/{resultId} to change it.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, [FromBody] CreateResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFoundMessage($"Enrolment {enrolmentId} was not found.");
        var organiserId = RequireUserId();
        if (enrolment.Category!.Event!.OrganiserId != organiserId) return Forbidden("You can only record results for events that you created.");
        if (enrolment.Result is not null) return ConflictMessage("A result has already been recorded for this enrolment.");
        if (enrolment.Status == EnrolmentStatuses.Cancelled) return BadRequestMessage("Results cannot be recorded for a cancelled enrolment.");

        var status = Lookup.Normalise(request.Status, ResultStatuses.All);
        if (status is null) return BadRequestMessage("status must be Finished, DNF, DNS or DQ.");

        var error = await ValidateFinishedResult(status, request.FinishTime, request.Position, enrolment.CategoryId, excludeResultId: null);
        if (error is not null) return BadRequestMessage(error);

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            Status = status,
            FinishTime = request.FinishTime,
            Position = request.Position,
            RecordedById = organiserId,
            RecordedAt = DateTime.Now
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        result.Enrolment = enrolment;
        return StatusCode(StatusCodes.Status201Created, result.ToResponse(await CountFinishers(enrolment.CategoryId)));
    }

    /// <summary>Correct an existing result. Only the Organiser who owns the event may do this.</summary>
    /// <remarks>Only the fields included in the body are changed.</remarks>
    /// <param name="resultId">The result ID.</param>
    /// <param name="request">The result fields to correct.</param>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The result does not exist.</response>
    [HttpPut("api/results/{resultId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int resultId, [FromBody] UpdateResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == resultId);

        if (result is null) return NotFoundMessage($"Result {resultId} was not found.");
        var organiserId = RequireUserId();
        if (result.Enrolment!.Category!.Event!.OrganiserId != organiserId) return Forbidden("You can only update results for events that you created.");

        var status = result.Status;
        if (request.Status is not null)
        {
            var s = Lookup.Normalise(request.Status, ResultStatuses.All);
            if (s is null) return BadRequestMessage("status must be Finished, DNF, DNS or DQ.");
            status = s;
        }

        var finishTime = request.FinishTime ?? result.FinishTime;
        var position = request.Position ?? result.Position;

        var error = await ValidateFinishedResult(status, finishTime, position, result.Enrolment.CategoryId, excludeResultId: resultId);
        if (error is not null) return BadRequestMessage(error);

        result.Status = status;
        result.FinishTime = finishTime;
        result.Position = position;
        result.RecordedById = organiserId;
        result.RecordedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return Ok(result.ToResponse(await CountFinishers(result.Enrolment.CategoryId)));
    }

    /// <summary>Public leaderboard for an event, grouped by category.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">Optional: only one category.</param>
    /// <param name="sortBy">"position" (default) or "time".</param>
    /// <response code="200">Array of categories, each with its ordered results.</response>
    /// <response code="400">Invalid sort_by value.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<CategoryResultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(
        int eventId,
        [FromQuery(Name = "category_id")] int? categoryId,
        [FromQuery(Name = "sort_by")] string? sortBy)
    {
        var sort = string.IsNullOrWhiteSpace(sortBy) ? "position" : sortBy.Trim().ToLowerInvariant();
        if (sort is not ("position" or "time")) return BadRequestMessage("sort_by must be 'position' or 'time'.");

        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null || (ev.Status == EventStatuses.Draft && ev.OrganiserId != CurrentUserId))
            return NotFoundMessage($"Event {eventId} was not found.");

        var query = _db.Results
            .AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Category).ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.Category!.EventId == eventId);

        if (categoryId is not null) query = query.Where(r => r.Enrolment!.CategoryId == categoryId);

        var results = await query.ToListAsync();

        // Group by ID, not by object: no-tracking queries can return separate Category instances per row.
        var grouped = results
            .GroupBy(r => r.Enrolment!.CategoryId)
            .Select(g => new { Category = g.First().Enrolment!.Category!, Results = g.ToList() })
            .OrderBy(g => g.Category.DistanceKm)
            .Select(x =>
            {
                var g = x.Results;
                var finishers = g.Count(r => r.Status == ResultStatuses.Finished);
                // Finished results first, ordered by position or time; DNF/DNS/DQ afterwards.
                var ordered = g
                    .OrderBy(r => r.Status == ResultStatuses.Finished ? 0 : 1)
                    .ThenBy(r => sort == "time" ? (r.FinishTime ?? TimeSpan.MaxValue).Ticks : (r.Position ?? int.MaxValue))
                    .Select(r => r.ToResponse(finishers))
                    .ToList();

                return new CategoryResultsResponse
                {
                    CategoryId = x.Category.CategoryId,
                    CategoryName = x.Category.Name,
                    DistanceKm = x.Category.DistanceKm,
                    Results = ordered
                };
            })
            .ToList();

        return Ok(grouped);
    }

    /// <summary>The logged-in Participant's personal race history across all events.</summary>
    /// <remarks>Each result includes total_finishers so a front end can show e.g. "47th out of 312 finishers".</remarks>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="limit">Items per page (1 to 100).</param>
    /// <response code="200">Array of results with event and category details, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/results/my")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var participantId = RequireUserId();
        var (skip, take) = Paging(page, limit);

        var results = await _db.Results
            .AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(en => en!.Participant)
            .Include(r => r.Enrolment).ThenInclude(en => en!.Category).ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
            .Skip(skip).Take(take)
            .ToListAsync();

        var categoryIds = results.Select(r => r.Enrolment!.CategoryId).Distinct().ToList();
        var finisherCounts = await _db.Results
            .Where(r => categoryIds.Contains(r.Enrolment!.CategoryId) && r.Status == ResultStatuses.Finished)
            .GroupBy(r => r.Enrolment!.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return Ok(results
            .Select(r => r.ToResponse(finisherCounts.GetValueOrDefault(r.Enrolment!.CategoryId)))
            .ToList());
    }

    private async Task<string?> ValidateFinishedResult(string status, TimeSpan? finishTime, int? position, int categoryId, int? excludeResultId)
    {
        if (finishTime is { } t && (t <= TimeSpan.Zero || t >= TimeSpan.FromDays(1)))
            return "finish_time must be between 00:00:01 and 23:59:59.";

        if (status != ResultStatuses.Finished) return null;

        if (finishTime is null) return "finish_time is required when status is Finished.";
        if (position is null) return "position is required when status is Finished.";
        if (position < 1) return "position must be 1 or higher.";

        var positionTaken = await _db.Results.AnyAsync(r =>
            r.Enrolment!.CategoryId == categoryId
            && r.Position == position
            && r.Status == ResultStatuses.Finished
            && (excludeResultId == null || r.ResultId != excludeResultId));

        return positionTaken ? $"Position {position} is already assigned in this category." : null;
    }

    private Task<int> CountFinishers(int categoryId) =>
        _db.Results.CountAsync(r => r.Enrolment!.CategoryId == categoryId && r.Status == ResultStatuses.Finished);
}
