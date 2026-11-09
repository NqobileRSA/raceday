using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Event detail page (public) and the Participant's Enter Event action.</summary>
public class EventsController : AppController
{
    public EventsController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await Api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!result.Success) return ApiFailure(result, RedirectToAction("Index", "Home"));

        var model = new EventDetailsViewModel { Event = result.Data! };
        await LoadExtras(model);
        return View(model);
    }

    /// <summary>Records the enrolment immediately through the API.</summary>
    [HttpPost]
    [RequireRole(AppRoles.Participant)]
    public async Task<IActionResult> Enrol(int id, EnrolInput enrol)
    {
        if (!ModelState.IsValid)
        {
            Flash(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "warning");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await Api.PostAsync<EnrolmentDto>($"api/events/{id}/categories/{enrol.CategoryId}/enrol", new
        {
            EmergencyContactName = string.IsNullOrWhiteSpace(enrol.EmergencyContactName) ? null : enrol.EmergencyContactName,
            EmergencyContactPhone = string.IsNullOrWhiteSpace(enrol.EmergencyContactPhone) ? null : enrol.EmergencyContactPhone
        });

        if (!result.Success) return ApiFailure(result, RedirectToAction(nameof(Details), new { id }));

        Flash($"You're in. Your entry for {result.Data!.CategoryName} is pending confirmation by the organiser.");
        return RedirectToAction("MyEvents", "Participant");
    }

    private async Task LoadExtras(EventDetailsViewModel model)
    {
        var user = CurrentUser;
        if (user is null) return;

        if (user.IsParticipant)
        {
            var mine = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/my?limit=100");
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == model.Event.EventId && e.Status != "Cancelled");
        }

        // Race-day weather is a nice-to-have: if the weather service is down the page still renders.
        if (model.Event.Route?.StartLatitude is not null && model.Event.IsUpcoming)
        {
            var weather = await Api.GetAsync<WeatherDto>($"api/events/{model.Event.EventId}/weather");
            if (weather.Success) model.Weather = weather.Data;
        }
    }
}
