using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects MVC pages using the session. Not logged in: redirect to the login page (with a return URL).
/// Logged in with the wrong role: redirect to the access denied page.
/// <c>[RequireRole]</c> with no roles means any logged-in user.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    public RequireRoleAttribute(params string[] roles) => Roles = roles;

    public string[] Roles { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.CurrentUser();
        if (user is null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (Roles.Length > 0 && !Roles.Contains(user.Role))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
