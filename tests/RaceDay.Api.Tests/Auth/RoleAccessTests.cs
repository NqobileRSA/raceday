using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Auth;

/// <summary>
/// Verifies that unauthenticated requests get 401, that the wrong role gets 403, and that every
/// role-based endpoint is actually protected with the correct role.
/// </summary>
public class RoleAccessTests
{
    private static AuthorizationFilterContext RunFilter(SessionAuthorizeAttribute filter, int? userId, string? role)
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        if (userId is not null) httpContext.Session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role is not null) httpContext.Session.SetString(SessionKeys.Role, role);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        filter.OnAuthorization(context);
        return context;
    }

    [Fact]
    public void NoSession_IsRejectedWith401()
    {
        var context = RunFilter(new SessionAuthorizeAttribute(Roles.Organiser), userId: null, role: null);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void AnyLoggedInUser_PassesWhenNoRoleRequired()
    {
        var context = RunFilter(new SessionAuthorizeAttribute(), userId: 7, role: Roles.Participant);

        Assert.Null(context.Result);
    }

    [Fact]
    public void Participant_CallingOrganiserEndpoint_IsRejectedWith403()
    {
        var context = RunFilter(new SessionAuthorizeAttribute(Roles.Organiser), userId: 3, role: Roles.Participant);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void Organiser_CallingParticipantEndpoint_IsRejectedWith403()
    {
        var context = RunFilter(new SessionAuthorizeAttribute(Roles.Participant), userId: 1, role: Roles.Organiser);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void Organiser_CallingOrganiserEndpoint_IsAllowed()
    {
        var context = RunFilter(new SessionAuthorizeAttribute(Roles.Organiser), userId: 1, role: Roles.Organiser);

        Assert.Null(context.Result);
    }

    [Fact]
    public void SessionWithUserIdButNoRole_IsRejectedWith403ForRoleEndpoints()
    {
        var context = RunFilter(new SessionAuthorizeAttribute(Roles.Organiser), userId: 1, role: null);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    /// <summary>Every endpoint that the plan marks as role-specific, and the role it must require.</summary>
    public static IEnumerable<object[]> ProtectedEndpoints => new List<object[]>
    {
        new object[] { typeof(EventsController), nameof(EventsController.Create), Roles.Organiser },
        new object[] { typeof(EventsController), nameof(EventsController.Update), Roles.Organiser },
        new object[] { typeof(EventsController), nameof(EventsController.Delete), Roles.Organiser },
        new object[] { typeof(EventsController), nameof(EventsController.GetMine), Roles.Organiser },
        new object[] { typeof(CategoriesController), nameof(CategoriesController.Create), Roles.Organiser },
        new object[] { typeof(CategoriesController), nameof(CategoriesController.Update), Roles.Organiser },
        new object[] { typeof(CategoriesController), nameof(CategoriesController.Delete), Roles.Organiser },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), Roles.Participant },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.GetMine), Roles.Participant },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.Cancel), Roles.Participant },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.GetForEvent), Roles.Organiser },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.Confirm), Roles.Organiser },
        new object[] { typeof(ResultsController), nameof(ResultsController.Create), Roles.Organiser },
        new object[] { typeof(ResultsController), nameof(ResultsController.Update), Roles.Organiser },
        new object[] { typeof(ResultsController), nameof(ResultsController.GetMine), Roles.Participant },
        new object[] { typeof(RoutesController), nameof(RoutesController.Create), Roles.Organiser },
        new object[] { typeof(RoutesController), nameof(RoutesController.Update), Roles.Organiser },
        new object[] { typeof(AnnouncementsController), nameof(AnnouncementsController.Create), Roles.Organiser },
        new object[] { typeof(AnnouncementsController), nameof(AnnouncementsController.Delete), Roles.Organiser },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public void RoleBasedEndpoint_RequiresCorrectRole(Type controller, string action, string expectedRole)
    {
        var method = controller.GetMethod(action, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<SessionAuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(new[] { expectedRole }, attribute!.Roles);
    }

    [Theory]
    [InlineData(typeof(UsersController))]
    public void ProfileEndpoints_RequireAnyLoggedInUser(Type controller)
    {
        var attribute = controller.GetCustomAttribute<SessionAuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Empty(attribute!.Roles);
    }

    [Fact]
    public void WeatherEndpoint_RequiresAnyLoggedInUser()
    {
        var attribute = typeof(WeatherController).GetMethod(nameof(WeatherController.Get))!
            .GetCustomAttribute<SessionAuthorizeAttribute>();
        Assert.NotNull(attribute);
        Assert.Empty(attribute!.Roles);
    }
}
