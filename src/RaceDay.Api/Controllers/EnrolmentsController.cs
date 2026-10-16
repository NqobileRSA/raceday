using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Participants enter events by choosing a category; Organisers view and confirm entries for their events.</summary>
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enter the logged-in Participant into an event by selecting a category.</summary>
    /// <remarks>
    /// Records the link between the Participant, the event and the selected category (status Pending).
    /// Checks that the event is Published and has not happened yet, that the Participant meets the
    /// category's age and gender rules, that the category is not full, and that the Participant does not
    /// already have an active entry in any category of the same event.
    /// </remarks>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID within that event.</param>
    /// <param name="request">Optional emergency contact details.</param>
    /// <response code="201">The new enrolment record.</response>
    /// <response code="400">Not eligible (age, gender, category full, event closed or in the past).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">The event or category does not exist.</response>
    /// <response code="409">The Participant is already entered in this event.</response>
    [HttpPost("api/events/{eventId:int}/categories/{categoryId:int}/enrol")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, int categoryId, [FromBody] EnrolRequest? request)
    {
        request ??= new EnrolRequest();
        var participantId = RequireUserId();

        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.EventId == eventId);
        if (category is null) return NotFoundMessage($"Category {categoryId} was not found for event {eventId}.");

        var ev = category.Event!;
        if (ev.Status != EventStatuses.Published) return BadRequestMessage("This event is not open for entries.");
        if (ev.EventDate < Today) return BadRequestMessage("Entries are closed because this event has already taken place.");

        var participant = await _db.Users.FindAsync(participantId);
        if (participant is null) return Unauthorized(new MessageResponse("Your session is no longer valid. Please log in again."));

        var alreadyEntered = await _db.Enrolments.AnyAsync(en =>
            en.ParticipantId == participantId
            && en.Category!.EventId == eventId
            && en.Status != EnrolmentStatuses.Cancelled);
        if (alreadyEntered) return ConflictMessage("You are already entered in this event.");

        var eligibilityError = CheckEligibility(category, participant, ev.EventDate);
        if (eligibilityError is not null) return BadRequestMessage(eligibilityError);

        // A previously cancelled entry in the same category is re-activated, because the database allows
        // only one row per participant per category (UQ_Enrolment_Unique).
        var enrolment = category.Enrolments.FirstOrDefault(en => en.ParticipantId == participantId);
        if (enrolment is null)
        {
            enrolment = new Enrolment { ParticipantId = participantId, CategoryId = categoryId };
            _db.Enrolments.Add(enrolment);
        }

        enrolment.Status = EnrolmentStatuses.Pending;
        enrolment.EnrolmentDate = DateTime.Now;
        enrolment.BibNumber = null;
        enrolment.EmergencyContactName = request.EmergencyContactName?.Trim();
        enrolment.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();

        await _db.SaveChangesAsync();

        enrolment.Participant = participant;
        enrolment.Category = category;
        return StatusCode(StatusCodes.Status201Created, enrolment.ToResponse());
    }

    /// <summary>List the logged-in Participant's own enrolments with event and category details.</summary>
    /// <param name="status">Optional filter: Pending, Confirmed or Cancelled.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="limit">Items per page (1 to 100).</param>
    /// <response code="200">Array of the Participant's enrolments, soonest event first.</response>
    /// <response code="400">Invalid status filter.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/enrolments/my")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var participantId = RequireUserId();
        var query = _db.Enrolments
            .AsNoTracking()
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .Include(en => en.Result)
            .Where(en => en.ParticipantId == participantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = Lookup.Normalise(status, EnrolmentStatuses.All);
            if (s is null) return BadRequestMessage("status must be Pending, Confirmed or Cancelled.");
            query = query.Where(en => en.Status == s);
        }

        var (skip, take) = Paging(page, limit);
        var items = await query
            .OrderBy(en => en.Category!.Event!.EventDate)
            .Skip(skip).Take(take)
            .ToListAsync();

        return Ok(items.Select(en => en.ToResponse()).ToList());
    }

    /// <summary>Cancel one of the logged-in Participant's enrolments.</summary>
    /// <remarks>The row is kept with status Cancelled so the history is not lost. Not allowed once the event date has passed.</remarks>
    /// <param name="enrolmentId">The enrolment ID.</param>
    /// <response code="200">Confirmation message.</response>
    /// <response code="400">The event has already taken place, or the enrolment is already cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not the Participant who owns this enrolment.</response>
    /// <response code="404">The enrolment does not exist.</response>
    [HttpDelete("api/enrolments/{enrolmentId:int}")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int enrolmentId)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFoundMessage($"Enrolment {enrolmentId} was not found.");
        if (enrolment.ParticipantId != RequireUserId()) return Forbidden("You can only cancel your own enrolments.");
        if (enrolment.Status == EnrolmentStatuses.Cancelled) return BadRequestMessage("This enrolment is already cancelled.");
        if (enrolment.Category!.Event!.EventDate < Today) return BadRequestMessage("You cannot cancel an entry for an event that has already taken place.");

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse($"Your entry for '{enrolment.Category.Event.Title}' has been cancelled."));
    }

    /// <summary>List every enrolment for one event. Only the Organiser who owns the event may do this.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="status">Optional status filter: Pending, Confirmed or Cancelled.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="limit">Items per page (1 to 100).</param>
    /// <response code="200">Array of enrolments with participant names and the selected category.</response>
    /// <response code="400">Invalid status filter.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(
        int eventId,
        [FromQuery(Name = "category_id")] int? categoryId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFoundMessage($"Event {eventId} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only view enrolments for events that you created.");

        var query = _db.Enrolments
            .AsNoTracking()
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .Include(en => en.Result)
            .Where(en => en.Category!.EventId == eventId);

        if (categoryId is not null) query = query.Where(en => en.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = Lookup.Normalise(status, EnrolmentStatuses.All);
            if (s is null) return BadRequestMessage("status must be Pending, Confirmed or Cancelled.");
            query = query.Where(en => en.Status == s);
        }

        var total = await query.CountAsync();
        var (skip, take) = Paging(page, limit);
        var items = await query
            .OrderBy(en => en.Category!.Name)
            .ThenBy(en => en.Participant!.LastName)
            .Skip(skip).Take(take)
            .ToListAsync();

        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items.Select(en => en.ToResponse()).ToList());
    }

    /// <summary>Confirm a pending enrolment and optionally assign a bib number. Only the owning Organiser.</summary>
    /// <param name="enrolmentId">The enrolment ID.</param>
    /// <param name="request">Optional bib number to assign.</param>
    /// <response code="200">The updated enrolment (status Confirmed).</response>
    /// <response code="400">The enrolment is cancelled, or the bib number is already used in this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The enrolment does not exist.</response>
    [HttpPut("api/enrolments/{enrolmentId:int}/confirm")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(int enrolmentId, [FromBody] ConfirmEnrolmentRequest? request)
    {
        request ??= new ConfirmEnrolmentRequest();

        var enrolment = await _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category).ThenInclude(c => c!.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFoundMessage($"Enrolment {enrolmentId} was not found.");
        var ev = enrolment.Category!.Event!;
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only confirm enrolments for events that you created.");
        if (enrolment.Status == EnrolmentStatuses.Cancelled) return BadRequestMessage("A cancelled enrolment cannot be confirmed.");

        if (!string.IsNullOrWhiteSpace(request.BibNumber))
        {
            var bib = request.BibNumber.Trim();
            var bibTaken = await _db.Enrolments.AnyAsync(en =>
                en.EnrolmentId != enrolmentId
                && en.BibNumber == bib
                && en.Category!.EventId == ev.EventId);
            if (bibTaken) return BadRequestMessage($"Bib number {bib} is already assigned in this event.");
            enrolment.BibNumber = bib;
        }

        enrolment.Status = EnrolmentStatuses.Confirmed;
        await _db.SaveChangesAsync();
        return Ok(enrolment.ToResponse());
    }

    /// <summary>Applies the category's age, gender and capacity rules. Returns an error message or null.</summary>
    internal static string? CheckEligibility(Category category, User participant, DateOnly eventDate)
    {
        if (category.MinAge is not null || category.MaxAge is not null)
        {
            if (participant.DateOfBirth is null)
                return "This category has an age requirement. Add your date of birth to your profile first.";

            var age = AgeOn(participant.DateOfBirth.Value, eventDate);
            if (category.MinAge is not null && age < category.MinAge)
                return $"You must be at least {category.MinAge} on race day to enter this category.";
            if (category.MaxAge is not null && age > category.MaxAge)
                return $"This category is for participants aged {category.MaxAge} or younger on race day.";
        }

        if (category.GenderRestriction is not null &&
            !string.Equals(category.GenderRestriction, participant.Gender, StringComparison.OrdinalIgnoreCase))
            return $"This category is restricted to {category.GenderRestriction} participants.";

        if (category.MaxParticipants is not null)
        {
            var active = category.Enrolments.Count(en => en.Status != EnrolmentStatuses.Cancelled);
            if (active >= category.MaxParticipants) return "This category is full.";
        }

        return null;
    }

    /// <summary>Age in whole years on a given date.</summary>
    internal static int AgeOn(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (onDate < dateOfBirth.AddYears(age)) age--;
        return age;
    }
}
