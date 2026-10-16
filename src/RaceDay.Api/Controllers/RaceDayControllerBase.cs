using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

/// <summary>Shared helpers for reading the logged-in user from the session and returning consistent errors.</summary>
[ApiController]
[Produces("application/json")]
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>The logged-in user's ID, or null for anonymous requests.</summary>
    protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);

    /// <summary>The logged-in user's role, or null for anonymous requests.</summary>
    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    /// <summary>
    /// The logged-in user's ID. Only call this inside actions protected by [SessionAuthorize],
    /// where the filter has already guaranteed the session holds a user.
    /// </summary>
    protected int RequireUserId() =>
        CurrentUserId ?? throw new InvalidOperationException("No authenticated user in session.");

    protected ObjectResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new MessageResponse(message));

    protected NotFoundObjectResult NotFoundMessage(string message) => NotFound(new MessageResponse(message));

    protected BadRequestObjectResult BadRequestMessage(string message) => BadRequest(new MessageResponse(message));

    protected ConflictObjectResult ConflictMessage(string message) => Conflict(new MessageResponse(message));

    /// <summary>Clamps paging query values to sensible bounds.</summary>
    protected static (int Skip, int Take) Paging(int page, int limit)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);
        return ((page - 1) * limit, limit);
    }

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
