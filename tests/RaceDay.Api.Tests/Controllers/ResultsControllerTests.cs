using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

/// <summary>Organisers capture finish times and positions; Participants view their own results.</summary>
public class ResultsControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private ResultsController As(User? user) => _data.Controller(db => new ResultsController(db), user);

    private static CreateResultRequest Finished(int position, string time) => new()
    {
        Status = "Finished",
        Position = position,
        FinishTime = TimeSpan.Parse(time)
    };

    [Fact]
    public async Task Create_AsOwningOrganiser_Returns201AndSavesTimeAndPosition()
    {
        var result = await As(_data.Organiser).Create(_data.PastEnrolment.EnrolmentId, Finished(1, "00:58:12"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var saved = await _data.Db.Results.SingleAsync(r => r.EnrolmentId == _data.PastEnrolment.EnrolmentId);
        Assert.Equal(new TimeSpan(0, 58, 12), saved.FinishTime);
        Assert.Equal(1, saved.Position);
        Assert.Equal(_data.Organiser.UserId, saved.RecordedById);
    }

    [Fact]
    public async Task Create_SecondResultForSameEnrolment_Returns409()
    {
        await As(_data.Organiser).Create(_data.PastEnrolment.EnrolmentId, Finished(1, "00:58:12"));

        var result = await As(_data.Organiser).Create(_data.PastEnrolment.EnrolmentId, Finished(2, "01:00:00"));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_FinishedWithoutTime_Returns400()
    {
        var result = await As(_data.Organiser).Create(_data.PastEnrolment.EnrolmentId,
            new CreateResultRequest { Status = "Finished", Position = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ForAnotherOrganisersEvent_Returns403()
    {
        var result = await As(_data.OtherOrganiser).Create(_data.PastEnrolment.EnrolmentId, Finished(1, "00:58:12"));

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetMine_AsParticipant_ReturnsOwnResultsWithFinisherCount()
    {
        await As(_data.Organiser).Create(_data.PastEnrolment.EnrolmentId, Finished(1, "00:58:12"));

        var result = await As(_data.Participant).GetMine();

        var items = Assert.IsType<List<ResultResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        var mine = Assert.Single(items);
        Assert.Equal("Soweto Heritage Walk", mine.EventTitle);
        Assert.Equal(1, mine.Position);
        Assert.Equal(1, mine.TotalFinishers);
    }

    [Fact]
    public async Task Update_CorrectsFinishTime()
    {
        var created = (ResultResponse)((ObjectResult)await As(_data.Organiser)
            .Create(_data.PastEnrolment.EnrolmentId, Finished(1, "00:58:12"))).Value!;

        var result = await As(_data.Organiser).Update(created.ResultId, new UpdateResultRequest { FinishTime = new TimeSpan(0, 57, 59) });

        var body = Assert.IsType<ResultResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(new TimeSpan(0, 57, 59), body.FinishTime);
    }

    [Fact]
    public async Task GetForEvent_PublicLeaderboard_GroupsByCategory()
    {
        await As(_data.Organiser).Create(_data.PastEnrolment.EnrolmentId, Finished(1, "00:58:12"));

        var result = await As(null).GetForEvent(_data.PastEvent.EventId, null, null);

        var groups = Assert.IsType<List<CategoryResultsResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        var group = Assert.Single(groups);
        Assert.Equal(_data.PastCategory.CategoryId, group.CategoryId);
        Assert.Single(group.Results);
    }

    public void Dispose() => _data.Dispose();
}
