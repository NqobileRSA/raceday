using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

/// <summary>Participants enter events by selecting a category; the enrolment links participant, event and category.</summary>
public class EnrolmentsControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private EnrolmentsController As(User? user) => _data.Controller(db => new EnrolmentsController(db), user);

    private static EnrolRequest Contact() => new() { EmergencyContactName = "Nomsa Ndlovu", EmergencyContactPhone = "073 111 2233" };

    [Fact]
    public async Task Enrol_AsParticipant_Returns201AndRecordsParticipantEventAndCategory()
    {
        var result = await As(_data.Participant).Enrol(_data.UpcomingEvent.EventId, _data.HalfMarathon.CategoryId, Contact());

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<EnrolmentResponse>(created.Value);
        Assert.Equal(_data.Participant.UserId, body.ParticipantId);
        Assert.Equal(_data.UpcomingEvent.EventId, body.EventId);
        Assert.Equal(_data.HalfMarathon.CategoryId, body.CategoryId);
        Assert.Equal(EnrolmentStatuses.Pending, body.Status);

        // The row in the database links the participant to the selected category of this event.
        var saved = await _data.Db.Enrolments.Include(e => e.Category).SingleAsync(e => e.EnrolmentId == body.EnrolmentId);
        Assert.Equal(_data.Participant.UserId, saved.ParticipantId);
        Assert.Equal(_data.HalfMarathon.CategoryId, saved.CategoryId);
        Assert.Equal(_data.UpcomingEvent.EventId, saved.Category!.EventId);
        Assert.Equal("Nomsa Ndlovu", saved.EmergencyContactName);
    }

    [Fact]
    public async Task Enrol_TwiceInSameEvent_Returns409()
    {
        await As(_data.Participant).Enrol(_data.UpcomingEvent.EventId, _data.HalfMarathon.CategoryId, Contact());

        // A different category of the same event still counts as a duplicate entry.
        var result = await As(_data.Participant).Enrol(_data.UpcomingEvent.EventId, _data.TenK.CategoryId, Contact());

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns400()
    {
        var result = await As(_data.Participant).Enrol(_data.OtherOrganisersEvent.EventId, _data.FullCategory.CategoryId, Contact());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Enrol_BelowMinimumAge_Returns400()
    {
        // Half marathon has min_age 16 and the young participant is 14.
        var result = await As(_data.YoungParticipant).Enrol(_data.UpcomingEvent.EventId, _data.HalfMarathon.CategoryId, Contact());

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(await _data.Db.Enrolments.AnyAsync(e => e.ParticipantId == _data.YoungParticipant.UserId));
    }

    [Fact]
    public async Task Enrol_InPastEvent_Returns400()
    {
        var other = _data.AddUser("late@gmail.com", "Late", "Runner", Roles.Participant, new DateOnly(1990, 1, 1), "Male");
        await _data.Db.SaveChangesAsync();

        var result = await As(other).Enrol(_data.PastEvent.EventId, _data.PastCategory.CategoryId, Contact());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Enrol_CategoryFromDifferentEvent_Returns404()
    {
        var result = await As(_data.Participant).Enrol(_data.UpcomingEvent.EventId, _data.FullCategory.CategoryId, Contact());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheParticipantsOwnEnrolments()
    {
        await As(_data.Participant).Enrol(_data.UpcomingEvent.EventId, _data.TenK.CategoryId, Contact());

        var result = await As(_data.Participant).GetMine(null);

        var items = Assert.IsType<List<EnrolmentResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, items.Count); // the past event plus the new one
        Assert.All(items, e => Assert.Equal(_data.Participant.UserId, e.ParticipantId));
    }

    [Fact]
    public async Task Cancel_OwnUpcomingEnrolment_SetsStatusCancelled()
    {
        var created = (EnrolmentResponse)((ObjectResult)await As(_data.Participant)
            .Enrol(_data.UpcomingEvent.EventId, _data.TenK.CategoryId, Contact())).Value!;

        var result = await As(_data.Participant).Cancel(created.EnrolmentId);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(EnrolmentStatuses.Cancelled, (await _data.Db.Enrolments.FindAsync(created.EnrolmentId))!.Status);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403()
    {
        var created = (EnrolmentResponse)((ObjectResult)await As(_data.Participant)
            .Enrol(_data.UpcomingEvent.EventId, _data.TenK.CategoryId, Contact())).Value!;

        var result = await As(_data.YoungParticipant).Cancel(created.EnrolmentId);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetForEvent_AsOwningOrganiser_ReturnsEnrolmentsWithCategory()
    {
        await As(_data.Participant).Enrol(_data.UpcomingEvent.EventId, _data.HalfMarathon.CategoryId, Contact());

        var result = await As(_data.Organiser).GetForEvent(_data.UpcomingEvent.EventId, null, null);

        var items = Assert.IsType<List<EnrolmentResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        var only = Assert.Single(items);
        Assert.Equal("Sipho Ndlovu", only.ParticipantName);
        Assert.Equal("21.1km Half Marathon", only.CategoryName);
    }

    [Fact]
    public async Task GetForEvent_AsDifferentOrganiser_Returns403()
    {
        var result = await As(_data.OtherOrganiser).GetForEvent(_data.UpcomingEvent.EventId, null, null);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Confirm_AsOwningOrganiser_SetsConfirmedAndBibNumber()
    {
        var created = (EnrolmentResponse)((ObjectResult)await As(_data.Participant)
            .Enrol(_data.UpcomingEvent.EventId, _data.HalfMarathon.CategoryId, Contact())).Value!;

        var result = await As(_data.Organiser).Confirm(created.EnrolmentId, new ConfirmEnrolmentRequest { BibNumber = "JBM-2145" });

        var body = Assert.IsType<EnrolmentResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(EnrolmentStatuses.Confirmed, body.Status);
        Assert.Equal("JBM-2145", body.BibNumber);
    }

    [Theory]
    [InlineData(2000, 6, 18, 2026, 6, 17, 25)]
    [InlineData(2000, 6, 18, 2026, 6, 18, 26)]
    [InlineData(2012, 2, 29, 2026, 2, 27, 13)]
    public void AgeOn_CalculatesWholeYears(int by, int bm, int bd, int oy, int om, int od, int expected)
    {
        Assert.Equal(expected, EnrolmentsController.AgeOn(new DateOnly(by, bm, bd), new DateOnly(oy, om, od)));
    }

    public void Dispose() => _data.Dispose();
}
