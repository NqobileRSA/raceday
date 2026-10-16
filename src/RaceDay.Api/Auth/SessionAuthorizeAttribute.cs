using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Auth;

/// <summary>
/// Protects an endpoint using the server-side session created at login.
/// <list type="bullet">
///   <item>No user ID in the session: 401 Unauthorized.</item>
///   <item>Roles supplied and the session role is not one of them: 403 Forbidden.</item>
/// </list>
/// Usage: <c>[SessionAuthorize]</c> for any logged-in user, <c>[SessionAuthorize(Roles.Organiser)]</c> for one role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public SessionAuthorizeAttribute(params string[] roles)
    {
        Roles = roles ?? Array.Empty<string>();
    }

    /// <summary>The roles allowed to call the endpoint. Empty means any authenticated user.</summary>
    public string[] Roles { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);

        if (userId is null)
        {
            context.Result = new UnauthorizedObjectResult(
                new MessageResponse("You must be logged in to access this resource."));
            return;
        }

        if (Roles.Length == 0) return;

        var role = session.GetString(SessionKeys.Role);
        if (role is null || !Roles.Contains(role, StringComparer.Ordinal))
        {
            context.Result = new ObjectResult(
                new MessageResponse($"This action is only available to: {string.Join(", ", Roles)}."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
