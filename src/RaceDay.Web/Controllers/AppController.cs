using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers: current user, flash messages and consistent handling of API errors.</summary>
public abstract class AppController : Controller
{
    protected readonly ApiClient Api;

    protected AppController(ApiClient api) => Api = api;

    protected SessionUser? CurrentUser => HttpContext.Session.CurrentUser();

    protected void Flash(string message, string kind = "success")
    {
        TempData["Flash"] = message;
        TempData["FlashKind"] = kind;
    }

    /// <summary>
    /// Turns a failed API call into the right response: expired session goes to login, missing data to a
    /// friendly 404 page, anything else back to <paramref name="fallback"/> with the API's message.
    /// </summary>
    protected IActionResult ApiFailure<T>(ApiResult<T> result, IActionResult fallback)
    {
        switch (result.StatusCode)
        {
            case 401:
                HttpContext.Session.Clear();
                Flash("Your session has ended. Please log in again.", "warning");
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Path + Request.QueryString });
            case 403:
                return RedirectToAction("AccessDenied", "Account");
            case 404:
                Response.StatusCode = 404;
                return View("NotFound");
            default:
                Flash(result.Message, "danger");
                return fallback;
        }
    }

    /// <summary>Copies API validation messages onto the matching form fields (snake_case to PascalCase).</summary>
    protected void AddApiErrors<T>(ApiResult<T> result, ModelStateDictionary modelState)
    {
        foreach (var (field, messages) in result.FieldErrors)
        {
            var key = string.Concat(field.TrimStart('$', '.').Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
            foreach (var m in messages) modelState.AddModelError(key, m);
        }
        if (result.FieldErrors.Count == 0) modelState.AddModelError(string.Empty, result.Message);
    }

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
