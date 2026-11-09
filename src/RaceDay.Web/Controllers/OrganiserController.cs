using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Organiser-only pages: dashboard, event management (with banner upload), categories, enrolments and results.
/// Every action calls the API; ownership and business rules are enforced there.
/// </summary>
[RequireRole(AppRoles.Organiser)]
public class OrganiserController : AppController
{
    public OrganiserController(ApiClient api) : base(api) { }

    // ------------------------------------------------------------------ dashboard and event list
    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetAsync<List<EventDto>>("api/events/my?limit=100");
        if (!result.Success) return ApiFailure(result, View(new DashboardViewModel()));
        return View(new DashboardViewModel { Events = result.Data! });
    }

    public async Task<IActionResult> Events(string? status)
    {
        var url = "api/events/my?limit=100" + (string.IsNullOrEmpty(status) ? "" : $"&status={Uri.EscapeDataString(status)}");
        var result = await Api.GetAsync<List<EventDto>>(url);
        if (!result.Success) return ApiFailure(result, View(new List<EventDto>()));
        ViewBag.Status = status;
        return View(result.Data!.OrderByDescending(e => e.EventDate).ToList());
    }

    // ------------------------------------------------------------------ create / edit / delete
    [HttpGet]
    public IActionResult Create() => View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (model.EventDate is { } date && date.Date < DateTime.Today)
            ModelState.AddModelError(nameof(model.EventDate), "The event date can't be in the past.");
        if (!ModelState.IsValid) return View("EventForm", model);

        var result = await Api.PostAsync<EventDto>("api/events", ToApi(model));
        if (!result.Success)
        {
            if (result.StatusCode is 401 or 403) return ApiFailure(result, View("EventForm", model));
            AddApiErrors(result, ModelState);
            return View("EventForm", model);
        }

        var ev = result.Data!;
        var bannerMessage = await UploadBanner(ev.EventId, model.Banner);
        Flash(bannerMessage is null
            ? $"{ev.Title} has been created. Add categories so participants can enter."
            : $"{ev.Title} has been created, but the banner didn't upload: {bannerMessage}", bannerMessage is null ? "success" : "warning");
        return RedirectToAction(nameof(Categories), new { id = ev.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Events)));
        var ev = result.Data!;
        if (ev.OrganiserId != CurrentUser!.Id) return RedirectToAction("AccessDenied", "Account");

        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId,
            CurrentImageUrl = ev.ImageUrl,
            Title = ev.Title,
            Description = ev.Description,
            EventDate = ev.EventDate.ToDateTime(TimeOnly.MinValue),
            DistanceKm = ev.DistanceKm ?? ev.DistancesKm.DefaultIfEmpty().Max(),
            Location = ev.Location,
            Province = ev.Province,
            EventType = ev.EventType,
            Status = ev.Status,
            ContactEmail = ev.ContactEmail
        });
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View("EventForm", model);

        var result = await Api.PutAsync<EventDto>($"api/events/{id}", ToApi(model));
        if (!result.Success)
        {
            if (result.StatusCode is 401 or 403 or 404) return ApiFailure(result, View("EventForm", model));
            AddApiErrors(result, ModelState);
            return View("EventForm", model);
        }

        var bannerMessage = await UploadBanner(id, model.Banner);
        Flash(bannerMessage is null ? "Event updated." : $"Event updated, but the banner didn't upload: {bannerMessage}",
            bannerMessage is null ? "success" : "warning");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteAsync<MessageDto>($"api/events/{id}");
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Events)));
        Flash(result.Data?.Message ?? "Event deleted.");
        return RedirectToAction(nameof(Events));
    }

    // ------------------------------------------------------------------ categories
    [HttpGet]
    public async Task<IActionResult> Categories(int id)
    {
        var model = await LoadCategories(id);
        return model is null ? RedirectToAction(nameof(Events)) : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory(int id, [Bind(Prefix = "NewCategory")] CategoryInput input)
    {
        if (input.MinAge is { } min && input.MaxAge is { } max && min > max)
            ModelState.AddModelError("NewCategory.MinAge", "Min age can't be more than max age.");

        if (!ModelState.IsValid)
        {
            var model = await LoadCategories(id);
            if (model is null) return RedirectToAction(nameof(Events));
            model.NewCategory = input;
            return View(nameof(Categories), model);
        }

        var body = new
        {
            input.Name,
            input.DistanceKm,
            EntryFee = input.EntryFee ?? 0,
            input.MaxParticipants,
            input.MinAge,
            input.MaxAge,
            GenderRestriction = string.IsNullOrEmpty(input.GenderRestriction) ? "None" : input.GenderRestriction
        };
        var result = input.CategoryId is { } categoryId
            ? await Api.PutAsync<CategoryDto>($"api/events/{id}/categories/{categoryId}", body)
            : await Api.PostAsync<CategoryDto>($"api/events/{id}/categories", body);

        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Categories), new { id }));
        Flash(input.CategoryId is null ? $"{result.Data!.Name} added." : $"{result.Data!.Name} updated.");
        return RedirectToAction(nameof(Categories), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int categoryId)
    {
        var result = await Api.DeleteAsync<MessageDto>($"api/events/{id}/categories/{categoryId}");
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Categories), new { id }));
        Flash(result.Data?.Message ?? "Category deleted.");
        return RedirectToAction(nameof(Categories), new { id });
    }

    // ------------------------------------------------------------------ enrolments
    public async Task<IActionResult> Enrolments(int id, int? categoryId, string? status)
    {
        var ev = await Api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success) return ApiFailure(ev, RedirectToAction(nameof(Events)));

        var url = $"api/events/{id}/enrolments?limit=100"
                  + (categoryId is null ? "" : $"&category_id={categoryId}")
                  + (string.IsNullOrEmpty(status) ? "" : $"&status={Uri.EscapeDataString(status)}");
        var list = await Api.GetAsync<List<EnrolmentDto>>(url);
        if (!list.Success) return ApiFailure(list, RedirectToAction(nameof(Events)));

        return View(new EnrolmentsViewModel
        {
            Event = ev.Data!,
            Categories = ev.Data!.Categories,
            Enrolments = list.Data!,
            CategoryId = categoryId,
            Status = status
        });
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(int id, int enrolmentId, string? bibNumber)
    {
        var result = await Api.PutAsync<EnrolmentDto>($"api/enrolments/{enrolmentId}/confirm",
            new { BibNumber = string.IsNullOrWhiteSpace(bibNumber) ? null : bibNumber.Trim().ToUpperInvariant() });
        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Enrolments), new { id }));
        Flash($"{result.Data!.ParticipantName} is confirmed{(string.IsNullOrEmpty(result.Data.BibNumber) ? "" : $" with bib {result.Data.BibNumber}")}.");
        return RedirectToAction(nameof(Enrolments), new { id });
    }

    // ------------------------------------------------------------------ results
    public async Task<IActionResult> Results(int id)
    {
        var ev = await Api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success) return ApiFailure(ev, RedirectToAction(nameof(Events)));

        var enrolments = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments?limit=100");
        if (!enrolments.Success) return ApiFailure(enrolments, RedirectToAction(nameof(Events)));

        var results = await Api.GetAsync<List<CategoryResultsDto>>($"api/events/{id}/results");
        var byEnrolment = (results.Data ?? new()).SelectMany(c => c.Results).ToDictionary(r => r.EnrolmentId);

        return View(new ResultsEntryViewModel
        {
            Event = ev.Data!,
            Rows = enrolments.Data!
                .Where(e => e.Status != "Cancelled")
                .OrderBy(e => e.CategoryName).ThenBy(e => byEnrolment.GetValueOrDefault(e.EnrolmentId)?.Position ?? int.MaxValue).ThenBy(e => e.ParticipantName)
                .Select(e => new ResultRow { Enrolment = e, Result = byEnrolment.GetValueOrDefault(e.EnrolmentId) })
                .ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultInput input)
    {
        var back = RedirectToAction(nameof(Results), new { id = input.EventId });
        if (input.Status == "Finished" && (string.IsNullOrEmpty(input.FinishTime) || input.Position is null))
            ModelState.AddModelError(nameof(input.FinishTime), "A finisher needs both a finish time and a position.");
        if (!ModelState.IsValid)
        {
            Flash(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "warning");
            return back;
        }

        TimeSpan? time = TimeSpan.TryParseExact(input.FinishTime, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var t) ? t : null;
        var finished = input.Status == "Finished";
        var body = new { input.Status, FinishTime = finished ? time : null, Position = finished ? input.Position : null };

        var result = input.ResultId is { } resultId
            ? await Api.PutAsync<ResultDto>($"api/results/{resultId}", body)
            : await Api.PostAsync<ResultDto>($"api/enrolments/{input.EnrolmentId}/results", body);

        if (!result.Success) return ApiFailure(result, back);
        var r = result.Data!;
        Flash(r.Status == "Finished"
            ? $"Saved: {r.ParticipantName} finished {Ordinal(r.Position ?? 0)} in {r.FinishTime:hh\\:mm\\:ss}."
            : $"Saved: {r.ParticipantName} marked {r.Status}.");
        return back;
    }

    // ------------------------------------------------------------------ helpers
    private async Task<CategoriesViewModel?> LoadCategories(int id)
    {
        var ev = await Api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!ev.Success)
        {
            Flash(ev.Message, "danger");
            return null;
        }
        if (ev.Data!.OrganiserId != CurrentUser!.Id)
        {
            Flash("You can only manage categories for events you created.", "danger");
            return null;
        }
        return new CategoriesViewModel { Event = ev.Data, Categories = ev.Data.Categories };
    }

    /// <summary>Uploads the banner through the API. Returns an error message, or null on success or when no file was chosen.</summary>
    private async Task<string?> UploadBanner(int eventId, IFormFile? banner)
    {
        if (banner is null || banner.Length == 0) return null;
        var upload = await Api.PostFileAsync<EventDto>($"api/events/{eventId}/banner", banner);
        return upload.Success ? null : upload.Message;
    }

    private static object ToApi(EventFormViewModel m) => new
    {
        m.Title,
        Description = m.Description ?? "",
        EventDate = DateOnly.FromDateTime(m.EventDate!.Value),
        m.DistanceKm,
        m.Location,
        Province = m.Province ?? "",
        m.EventType,
        m.Status,
        ContactEmail = string.IsNullOrWhiteSpace(m.ContactEmail) ? null : m.ContactEmail
    };

    public static string Ordinal(int n) => n <= 0 ? "-" : n + ((n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });
}
