using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Participant-only pages: my events, my results and profile.</summary>
[RequireRole(AppRoles.Participant)]
public class ParticipantController : AppController
{
    public ParticipantController(ApiClient api) : base(api) { }

    public async Task<IActionResult> MyEvents()
    {
        var result = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/my?limit=100");
        if (!result.Success) return ApiFailure(result, View(new List<EnrolmentDto>()));
        return View(result.Data!);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await Api.DeleteAsync<MessageDto>($"api/enrolments/{id}");
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(MyEvents)));
        Flash(result.Data?.Message ?? "Your entry has been cancelled.");
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await Api.GetAsync<List<ResultDto>>("api/results/my?limit=100");
        if (!result.Success) return ApiFailure(result, View(new MyResultsViewModel()));
        return View(new MyResultsViewModel { Results = result.Data! });
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetAsync<UserDto>("api/users/me");
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "Home"));
        return View(ToForm(result.Data!));
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (model.DateOfBirth is { } dob && dob.Date > DateTime.Today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth can't be in the future.");

        if (!ModelState.IsValid)
        {
            model.User = (await Api.GetAsync<UserDto>("api/users/me")).Data ?? new UserDto();
            return View(model);
        }

        var result = await Api.PutAsync<UserDto>("api/users/me", new
        {
            model.FirstName,
            model.LastName,
            Phone = model.Phone ?? "",
            DateOfBirth = model.DateOfBirth is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null,
            Gender = string.IsNullOrEmpty(model.Gender) ? null : model.Gender,
            Province = model.Province ?? ""
        });

        if (!result.Success)
        {
            if (result.StatusCode is 401 or 403) return ApiFailure(result, View(model));
            AddApiErrors(result, ModelState);
            model.User = (await Api.GetAsync<UserDto>("api/users/me")).Data ?? new UserDto();
            return View(model);
        }

        HttpContext.Session.UpdateProfile(result.Data!);
        Flash("Your profile has been updated.");
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>Sends the picture to the API, which stores it in Azure Blob Storage and saves the URL.</summary>
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Flash("Choose a picture to upload.", "warning");
            return RedirectToAction(nameof(Profile));
        }

        var result = await Api.PostFileAsync<UserDto>("api/users/me/profile-picture", picture);
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Profile)));

        HttpContext.Session.UpdateProfile(result.Data!);
        Flash("Your profile picture has been updated.");
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToForm(UserDto u) => new()
    {
        User = u,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Phone = u.Phone,
        DateOfBirth = u.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
        Gender = u.Gender,
        Province = u.Province
    };
}
