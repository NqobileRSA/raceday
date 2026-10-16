using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

/// <summary>Organisers define age or distance categories; both roles can view them.</summary>
public class CategoriesControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private CategoriesController As(User? user) => _data.Controller(db => new CategoriesController(db), user);

    [Fact]
    public async Task Create_AgeCategoryOnOwnEvent_Returns201()
    {
        var request = new CreateCategoryRequest { Name = "Under 20 - 10km", DistanceKm = 10m, MinAge = 12, MaxAge = 19, EntryFee = 80m };

        var result = await As(_data.Organiser).Create(_data.UpcomingEvent.EventId, request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<CategoryResponse>(created.Value);
        Assert.Equal(_data.UpcomingEvent.EventId, body.EventId);
        Assert.Equal(19, body.MaxAge);
    }

    [Fact]
    public async Task Create_OnAnotherOrganisersEvent_Returns403()
    {
        var result = await As(_data.Organiser).Create(_data.OtherOrganisersEvent.EventId,
            new CreateCategoryRequest { Name = "Senior", DistanceKm = 40m });

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Create_WithMinAgeAboveMaxAge_Returns400()
    {
        var result = await As(_data.Organiser).Create(_data.UpcomingEvent.EventId,
            new CreateCategoryRequest { Name = "Bad ages", DistanceKm = 5m, MinAge = 40, MaxAge = 20 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsCategoriesWithEnrolmentCounts()
    {
        var result = await As(null).GetAll(_data.OtherOrganisersEvent.EventId);

        var categories = Assert.IsType<List<CategoryResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        var full = Assert.Single(categories);
        Assert.Equal(1, full.EnrolmentCount);
        Assert.Equal(0, full.SpotsRemaining);
    }

    [Fact]
    public async Task GetAll_ForMissingEvent_Returns404()
    {
        var result = await As(null).GetAll(9999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    public void Dispose() => _data.Dispose();
}
