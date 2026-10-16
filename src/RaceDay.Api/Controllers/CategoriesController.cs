using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Age and distance categories within an event, e.g. "Under 20", "Senior", "10km", "21km".</summary>
[Route("api/events/{eventId:int}/categories")]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List the categories for an event, with current enrolment counts.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="200">Array of categories ordered by distance.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFoundMessage($"Event {eventId} was not found.");

        var categories = await _db.Categories
            .AsNoTracking()
            .Include(c => c.Enrolments)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm).ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(categories.Select(c => c.ToResponse()).ToList());
    }

    /// <summary>Add a category to an event. Only the owning Organiser may do this.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="request">The category name, distance and optional fee, capacity, age and gender rules.</param>
    /// <response code="201">The new category.</response>
    /// <response code="400">Validation failed (e.g. min_age greater than max_age, invalid gender restriction).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CreateCategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFoundMessage($"Event {eventId} was not found.");
        if (ev.OrganiserId != RequireUserId()) return Forbidden("You can only add categories to events that you created.");

        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequestMessage("name is required.");
        if (request.DistanceKm is null or <= 0) return BadRequestMessage("distance_km must be greater than 0.");

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            DistanceKm = request.DistanceKm.Value,
            EntryFee = request.EntryFee ?? 0m,
            MaxParticipants = request.MaxParticipants,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
        };

        var error = ApplyGenderRestriction(category, request.GenderRestriction) ?? ValidateAges(category);
        if (error is not null) return BadRequestMessage(error);

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, category.ToResponse());
    }

    /// <summary>Update a category. Only the owning Organiser may do this.</summary>
    /// <remarks>Only the fields included in the body are changed. max_participants cannot be set below the current number of active enrolments.</remarks>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="request">The category fields to change.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event or category does not exist.</response>
    [HttpPut("{categoryId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int eventId, int categoryId, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.EventId == eventId);

        if (category is null) return NotFoundMessage($"Category {categoryId} was not found for event {eventId}.");
        if (category.Event!.OrganiserId != RequireUserId()) return Forbidden("You can only update categories on events that you created.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequestMessage("name cannot be empty.");
            category.Name = request.Name.Trim();
        }

        if (request.DistanceKm is not null) category.DistanceKm = request.DistanceKm.Value;
        if (request.EntryFee is not null) category.EntryFee = request.EntryFee.Value;
        if (request.MinAge is not null) category.MinAge = request.MinAge;
        if (request.MaxAge is not null) category.MaxAge = request.MaxAge;

        if (request.MaxParticipants is not null)
        {
            var active = category.Enrolments.Count(en => en.Status != EnrolmentStatuses.Cancelled);
            if (request.MaxParticipants < active)
                return BadRequestMessage($"max_participants cannot be lower than the {active} active enrolments.");
            category.MaxParticipants = request.MaxParticipants;
        }

        var error = (request.GenderRestriction is null ? null : ApplyGenderRestriction(category, request.GenderRestriction))
                    ?? ValidateAges(category);
        if (error is not null) return BadRequestMessage(error);

        await _db.SaveChangesAsync();
        return Ok(category.ToResponse());
    }

    /// <summary>Delete a category and its enrolments. Only the owning Organiser may do this.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <response code="200">Confirmation message.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the Organiser who owns this event.</response>
    /// <response code="404">The event or category does not exist.</response>
    [HttpDelete("{categoryId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.EventId == eventId);

        if (category is null) return NotFoundMessage($"Category {categoryId} was not found for event {eventId}.");
        if (category.Event!.OrganiserId != RequireUserId()) return Forbidden("You can only delete categories on events that you created.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse($"Category '{category.Name}' and its enrolments were deleted."));
    }

    /// <summary>Sets the gender restriction. "None" or an empty string clears it.</summary>
    private static string? ApplyGenderRestriction(Category category, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            category.GenderRestriction = null;
            return null;
        }

        var g = Lookup.Normalise(value, Genders.Restrictions);
        if (g is null) return "gender_restriction must be 'Male', 'Female' or 'None'.";
        category.GenderRestriction = g;
        return null;
    }

    private static string? ValidateAges(Category category) =>
        category.MinAge is not null && category.MaxAge is not null && category.MinAge > category.MaxAge
            ? "min_age cannot be greater than max_age."
            : null;
}
