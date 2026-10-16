using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

/// <summary>Event management: Organisers create, update and delete their own events; everyone can browse.</summary>
public class EventsControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private EventsController As(User? user) => _data.Controller(db => new EventsController(db), user);

    private static CreateEventRequest NewEvent() => new()
    {
        Title = "Durban Beachfront 10k",
        Description = "Flat and fast along the Golden Mile.",
        EventDate = TestData.Today.AddDays(90),
        Location = "uShaka Marine World, Durban",
        Province = "KwaZulu-Natal",
        EventType = "running",
        Status = "Published",
        ContactEmail = "info@durban10k.co.za"
    };

    [Fact]
    public async Task Create_AsOrganiser_Returns201AndLinksEventToOrganiser()
    {
        var result = await As(_data.Organiser).Create(NewEvent());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var body = Assert.IsType<EventResponse>(created.Value);
        Assert.Equal(_data.Organiser.UserId, body.OrganiserId);
        Assert.Equal("Running", body.EventType); // normalised to the CHECK constraint spelling

        var saved = await _data.Db.Events.SingleAsync(e => e.EventId == body.EventId);
        Assert.Equal("Durban Beachfront 10k", saved.Title);
        Assert.Equal(_data.Organiser.UserId, saved.OrganiserId);
    }

    [Fact]
    public async Task Create_WithInvalidEventType_Returns400()
    {
        var request = NewEvent();
        request.EventType = "Swimming";

        var result = await As(_data.Organiser).Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithPastDate_Returns400()
    {
        var request = NewEvent();
        request.EventDate = TestData.Today.AddDays(-1);

        var result = await As(_data.Organiser).Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_OwnEvent_Returns200AndSavesChanges()
    {
        var result = await As(_data.Organiser).Update(_data.UpcomingEvent.EventId,
            new UpdateEventRequest { Title = "Joburg City Marathon 2026", Location = "FNB Stadium" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Joburg City Marathon 2026", Assert.IsType<EventResponse>(ok.Value).Title);
        Assert.Equal("FNB Stadium", (await _data.Db.Events.FindAsync(_data.UpcomingEvent.EventId))!.Location);
    }

    [Fact]
    public async Task Update_AnotherOrganisersEvent_Returns403AndChangesNothing()
    {
        var result = await As(_data.Organiser).Update(_data.OtherOrganisersEvent.EventId,
            new UpdateEventRequest { Title = "Hijacked" });

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("Cape Town Coastal Cycle", (await _data.Db.Events.FindAsync(_data.OtherOrganisersEvent.EventId))!.Title);
    }

    [Fact]
    public async Task Update_MissingEvent_Returns404()
    {
        var result = await As(_data.Organiser).Update(9999, new UpdateEventRequest { Title = "Nope" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_OwnEvent_Returns200AndRemovesIt()
    {
        var id = _data.DraftEvent.EventId;

        var result = await As(_data.Organiser).Delete(id);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await _data.Db.Events.AnyAsync(e => e.EventId == id));
    }

    [Fact]
    public async Task Delete_AnotherOrganisersEvent_Returns403()
    {
        var result = await As(_data.Organiser).Delete(_data.OtherOrganisersEvent.EventId);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.True(await _data.Db.Events.AnyAsync(e => e.EventId == _data.OtherOrganisersEvent.EventId));
    }

    [Fact]
    public async Task GetAll_Public_ReturnsOnlyPublishedEvents()
    {
        var result = await As(null).GetAll(null, null, null, null);

        var events = Assert.IsType<List<EventResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.All(events, e => Assert.Equal(EventStatuses.Published, e.Status));
        Assert.DoesNotContain(events, e => e.EventId == _data.DraftEvent.EventId);
        Assert.Contains(events, e => e.EventId == _data.UpcomingEvent.EventId);
    }

    [Fact]
    public async Task GetAll_FilteredByEventType_ReturnsMatchingEventsOnly()
    {
        var result = await As(null).GetAll(null, "Cycling", null, null);

        var events = Assert.IsType<List<EventResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        var single = Assert.Single(events);
        Assert.Equal(_data.OtherOrganisersEvent.EventId, single.EventId);
    }

    [Fact]
    public async Task GetById_IncludesCategoriesAndDistances()
    {
        var result = await As(null).GetById(_data.UpcomingEvent.EventId);

        var detail = Assert.IsType<EventDetailResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, detail.Categories.Count);
        Assert.Equal(new List<decimal> { 10m, 21.1m }, detail.DistancesKm);
    }

    [Fact]
    public async Task GetById_DraftEvent_IsHiddenFromThePublic()
    {
        var result = await As(_data.Participant).GetById(_data.DraftEvent.EventId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheOrganisersOwnEvents()
    {
        var result = await As(_data.Organiser).GetMine(null);

        var events = Assert.IsType<List<EventResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal(_data.Organiser.UserId, e.OrganiserId));
    }

    public void Dispose() => _data.Dispose();
}
