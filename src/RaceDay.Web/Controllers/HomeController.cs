using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Public home page with upcoming events and filters, plus friendly error pages.</summary>
public class HomeController : AppController
{
    public HomeController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(string? province, string? eventType, DateTime? fromDate, DateTime? toDate)
    {
        var model = new HomeViewModel { Province = province, EventType = eventType, FromDate = fromDate, ToDate = toDate };
        if (fromDate is not null && toDate is not null && fromDate > toDate)
        {
            Flash("The 'from' date must be before the 'to' date.", "warning");
            return View(model);
        }

        // Only upcoming events: from today, or later if the user picked a later start date.
        var from = fromDate is { } f && DateOnly.FromDateTime(f) > Today ? DateOnly.FromDateTime(f) : Today;
        var query = new List<string> { $"from_date={from:yyyy-MM-dd}", "limit=100" };
        if (!string.IsNullOrEmpty(province)) query.Add($"province={Uri.EscapeDataString(province)}");
        if (!string.IsNullOrEmpty(eventType)) query.Add($"event_type={Uri.EscapeDataString(eventType)}");
        if (toDate is { } t) query.Add($"to_date={DateOnly.FromDateTime(t):yyyy-MM-dd}");

        var result = await Api.GetAsync<List<EventDto>>("api/events?" + string.Join("&", query));
        if (!result.Success) Flash(result.Message, "danger");
        model.Events = result.Data ?? new();
        return View(model);
    }

    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code) => code == 404 ? View("NotFound") : View("Error");

    public IActionResult Error() => View();
}
