using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

public class UsersControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private UsersController As(User user) => _data.Controller(db => new UsersController(db), user);

    [Fact]
    public async Task GetMe_ReturnsProfileWithoutPasswordHash()
    {
        var result = await As(_data.Participant).GetMe();

        var body = Assert.IsType<UserResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(_data.Participant.Email, body.Email);
        Assert.Null(typeof(UserResponse).GetProperty("PasswordHash"));
    }

    [Fact]
    public async Task UpdateMe_ChangesOnlySuppliedFields()
    {
        var result = await As(_data.Organiser).UpdateMe(new UpdateProfileRequest { Phone = "082 000 0000", Province = "Limpopo" });

        var body = Assert.IsType<UserResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("082 000 0000", body.Phone);
        Assert.Equal("Limpopo", body.Province);
        Assert.Equal("Thabo", body.FirstName);
    }

    [Fact]
    public async Task UpdateMe_WithInvalidGender_Returns400()
    {
        var result = await As(_data.Organiser).UpdateMe(new UpdateProfileRequest { Gender = "Robot" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    public void Dispose() => _data.Dispose();
}

public class WeatherControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private sealed class FakeWeather : IWeatherService
    {
        public bool Fail { get; init; }

        public Task<WeatherResponse> GetCurrentAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new WeatherUnavailableException("The weather service is currently unavailable.");
            return Task.FromResult(new WeatherResponse
            {
                Latitude = latitude, Longitude = longitude, TemperatureC = 18.5m, HumidityPercent = 40,
                WindSpeedKmh = 12m, WeatherCode = 1, Conditions = "Mainly clear", Source = "Fake"
            });
        }
    }

    private WeatherController As(User user, bool fail = false) =>
        _data.Controller(db => new WeatherController(db, new FakeWeather { Fail = fail }), user);

    private async Task AddRoute()
    {
        _data.Db.Routes.Add(new EventRoute { EventId = _data.UpcomingEvent.EventId, StartLatitude = -26.2359m, StartLongitude = 27.9825m });
        await _data.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_WithRouteCoordinates_Returns200()
    {
        await AddRoute();

        var result = await As(_data.Participant).Get(_data.UpcomingEvent.EventId, CancellationToken.None);

        var body = Assert.IsType<WeatherResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(_data.UpcomingEvent.EventId, body.EventId);
        Assert.Equal(-26.2359m, body.Latitude);
    }

    [Fact]
    public async Task Get_WithoutRoute_Returns404()
    {
        var result = await As(_data.Participant).Get(_data.UpcomingEvent.EventId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_WhenProviderIsDown_Returns503()
    {
        await AddRoute();

        var result = await As(_data.Participant, fail: true).Get(_data.UpcomingEvent.EventId, CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, error.StatusCode);
    }

    public void Dispose() => _data.Dispose();
}

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Hash_IsSaltedSoTheSamePasswordGivesDifferentHashes()
    {
        var user = new User { Email = "a@b.co.za" };

        Assert.NotEqual(_service.Hash(user, "Password123!"), _service.Hash(user, "Password123!"));
    }

    [Fact]
    public void Verify_AcceptsCorrectAndRejectsWrongPassword()
    {
        var user = new User { Email = "a@b.co.za" };
        user.PasswordHash = _service.Hash(user, "Password123!");

        Assert.True(_service.Verify(user, "Password123!"));
        Assert.False(_service.Verify(user, "password123!"));
    }

    [Fact]
    public void Verify_SeedHash_MatchesDemoPassword()
    {
        var user = new User { PasswordHash = RaceDay.Api.Data.SeedData.DemoPasswordHash };

        Assert.True(_service.Verify(user, "Password123!"));
    }

    [Fact]
    public void Verify_NonIdentityHash_ReturnsFalseInsteadOfThrowing()
    {
        var user = new User { PasswordHash = "$2b$10$notARealIdentityHash" };

        Assert.False(_service.Verify(user, "Password123!"));
    }
}
