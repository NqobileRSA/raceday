using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. All credential checks happen in the API.</summary>
public class AccountController : AppController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (CurrentUser is { } user) return RedirectHome(user);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PostAsync<AuthResponseDto>("api/auth/login", new { model.Email, model.Password });
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.StatusCode == 401 ? "That email and password don't match an account." : result.Message);
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data.User);
        Flash($"Welcome back, {result.Data.User.FirstName}.");

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return RedirectHome(HttpContext.Session.CurrentUser()!);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUser is { } user) return RedirectHome(user);
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.DateOfBirth is { } dob && dob.Date > DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth can't be in the future.");
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PostAsync<AuthResponseDto>("api/auth/register", new
        {
            model.Email,
            model.Password,
            model.FirstName,
            model.LastName,
            model.Role,
            model.Phone,
            DateOfBirth = model.DateOfBirth is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null,
            Gender = string.IsNullOrEmpty(model.Gender) ? null : model.Gender,
            Province = string.IsNullOrEmpty(model.Province) ? null : model.Province
        });

        if (!result.Success || result.Data is null)
        {
            if (result.StatusCode == 409) ModelState.AddModelError(nameof(model.Email), "An account with this email already exists. Try logging in.");
            else AddApiErrors(result, ModelState);
            return View(model);
        }

        // The API logs new users in straight away; mirror that in the MVC session.
        HttpContext.Session.SignIn(result.Data.User);
        Flash($"Welcome to RaceDay, {result.Data.User.FirstName}.");
        return RedirectHome(HttpContext.Session.CurrentUser()!);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await Api.PostAsync<MessageDto>("api/auth/logout");
        HttpContext.Session.Clear();
        Flash("You've been logged out.");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }

    private IActionResult RedirectHome(SessionUser user) => user.IsOrganiser
        ? RedirectToAction("Dashboard", "Organiser")
        : RedirectToAction("Index", "Home");
}
