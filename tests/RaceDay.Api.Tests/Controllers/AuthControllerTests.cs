using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

/// <summary>User registration and login, including session creation and password hashing.</summary>
public class AuthControllerTests : IDisposable
{
    private readonly TestData _data = new();

    private AuthController NewController() => _data.Controller(db => new AuthController(db, _data.Passwords));

    private static RegisterRequest ValidRegistration(string role = "Participant") => new()
    {
        Email = "Naledi.Khumalo@Gmail.com",
        Password = "Strong1234",
        FirstName = "Naledi",
        LastName = "Khumalo",
        Role = role,
        DateOfBirth = new DateOnly(1998, 2, 14),
        Gender = "Female",
        Province = "Gauteng"
    };

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Register_WithValidDetails_Returns201AndStartsSessionWithRole(string role)
    {
        var controller = NewController();

        var result = await controller.Register(ValidRegistration(role));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal(role, body.User.Role);
        Assert.Equal("naledi.khumalo@gmail.com", body.User.Email);

        var session = controller.HttpContext.Session;
        Assert.Equal(body.User.UserId, session.GetInt32(SessionKeys.UserId));
        Assert.Equal(role, session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var controller = NewController();

        await controller.Register(ValidRegistration());

        var saved = await _data.Db.Users.SingleAsync(u => u.Email == "naledi.khumalo@gmail.com");
        Assert.NotEqual("Strong1234", saved.PasswordHash);
        Assert.DoesNotContain("Strong1234", saved.PasswordHash);
        Assert.True(_data.Passwords.Verify(saved, "Strong1234"));
    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns409()
    {
        var request = ValidRegistration();
        request.Email = "PARTICIPANT@gmail.com"; // already seeded, different case

        var result = await NewController().Register(request);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400AndCreatesNoUser()
    {
        var request = ValidRegistration("Admin");
        var before = await _data.Db.Users.CountAsync();

        var result = await NewController().Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(before, await _data.Db.Users.CountAsync());
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("allletters")]
    [InlineData("12345678")]
    public async Task Register_WithWeakPassword_Returns400(string password)
    {
        var request = ValidRegistration();
        request.Password = password;

        var result = await NewController().Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithInvalidGender_Returns400()
    {
        var request = ValidRegistration();
        request.Gender = "Unknown";

        var result = await NewController().Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndStoresUserIdAndRoleInSession()
    {
        var controller = NewController();

        var result = await controller.Login(new LoginRequest { Email = _data.Organiser.Email, Password = TestData.Password });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal(_data.Organiser.UserId, body.User.UserId);
        Assert.Equal(_data.Organiser.UserId, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Organiser, controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401AndNoSession()
    {
        var controller = NewController();

        var result = await controller.Login(new LoginRequest { Email = _data.Participant.Email, Password = "WrongPassword1" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var result = await NewController().Login(new LoginRequest { Email = "nobody@raceday.co.za", Password = TestData.Password });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        var controller = _data.Controller(db => new AuthController(db, _data.Passwords), _data.Participant);

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    public void Dispose() => _data.Dispose();
}
