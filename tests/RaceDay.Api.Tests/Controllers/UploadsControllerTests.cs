using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests.Controllers;

/// <summary>Image uploads go through the API to blob storage, and the returned URL is saved to the database.</summary>
public class UploadsControllerTests : IDisposable
{
    private readonly TestData _data = new();
    private readonly FakeImageStorage _storage = new();

    private UploadsController As(User user) => _data.Controller(db => new UploadsController(db, _storage), user);

    // Smallest valid file signatures for each allowed type.
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 };

    private static ImageUploadRequest File(byte[] bytes, string contentType, string name = "banner.png")
    {
        var stream = new MemoryStream(bytes);
        return new ImageUploadRequest
        {
            File = new FormFile(stream, 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = contentType }
        };
    }

    [Fact]
    public async Task UploadEventBanner_AsOwner_StoresInBlobAndSavesUrl()
    {
        var result = await As(_data.Organiser).UploadEventBanner(_data.UpcomingEvent.EventId, File(Png, "image/png"), CancellationToken.None);

        var body = Assert.IsType<EventResponse>(Assert.IsType<OkObjectResult>(result).Value);
        var uploaded = Assert.Single(_storage.Uploads);
        Assert.Equal(ImageContainers.EventBanners, uploaded.Container);
        Assert.Equal(uploaded.Url, body.ImageUrl);
        Assert.Equal(uploaded.Url, (await _data.Db.Events.FindAsync(_data.UpcomingEvent.EventId))!.ImageUrl);
    }

    [Fact]
    public async Task UploadEventBanner_ReplacingBanner_DeletesOldBlob()
    {
        await As(_data.Organiser).UploadEventBanner(_data.UpcomingEvent.EventId, File(Png, "image/png"), CancellationToken.None);
        var first = _storage.Uploads[0].Url;

        await As(_data.Organiser).UploadEventBanner(_data.UpcomingEvent.EventId, File(Jpeg, "image/jpeg", "b.jpg"), CancellationToken.None);

        Assert.Contains(first, _storage.Deleted);
    }

    [Fact]
    public async Task UploadEventBanner_OnAnotherOrganisersEvent_Returns403AndUploadsNothing()
    {
        var result = await As(_data.Organiser).UploadEventBanner(_data.OtherOrganisersEvent.EventId, File(Png, "image/png"), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(_storage.Uploads);
    }

    [Fact]
    public async Task UploadEventBanner_WithNonImageFile_Returns400()
    {
        var fake = Encoding.UTF8.GetBytes("this is not really a picture");

        var result = await As(_data.Organiser).UploadEventBanner(_data.UpcomingEvent.EventId, File(fake, "image/png"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_storage.Uploads);
    }

    [Fact]
    public async Task UploadEventBanner_WithDisallowedType_Returns400()
    {
        var result = await As(_data.Organiser).UploadEventBanner(_data.UpcomingEvent.EventId, File(Png, "image/gif", "a.gif"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadEventBanner_WhenStorageIsDown_Returns503()
    {
        _storage.Fail = true;

        var result = await As(_data.Organiser).UploadEventBanner(_data.UpcomingEvent.EventId, File(Png, "image/png"), CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, error.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_SavesUrlOnTheLoggedInUser()
    {
        var result = await As(_data.Participant).UploadProfilePicture(File(Jpeg, "image/jpeg", "me.jpg"), CancellationToken.None);

        var body = Assert.IsType<UserResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(ImageContainers.ProfilePictures, _storage.Uploads[0].Container);
        Assert.Equal(_storage.Uploads[0].Url, body.ProfilePictureUrl);
        Assert.Equal(_storage.Uploads[0].Url, (await _data.Db.Users.FindAsync(_data.Participant.UserId))!.ProfilePictureUrl);
    }

    [Fact]
    public void UploadEndpoints_RequireTheRightRoles()
    {
        var banner = typeof(UploadsController).GetMethod(nameof(UploadsController.UploadEventBanner))!;
        var picture = typeof(UploadsController).GetMethod(nameof(UploadsController.UploadProfilePicture))!;

        Assert.Equal(new[] { Roles.Organiser }, ((RaceDay.Api.Auth.SessionAuthorizeAttribute)banner.GetCustomAttributes(typeof(RaceDay.Api.Auth.SessionAuthorizeAttribute), false)[0]).Roles);
        Assert.Empty(((RaceDay.Api.Auth.SessionAuthorizeAttribute)picture.GetCustomAttributes(typeof(RaceDay.Api.Auth.SessionAuthorizeAttribute), false)[0]).Roles);
    }

    public void Dispose() => _data.Dispose();

    private sealed class FakeImageStorage : IImageStorage
    {
        public List<(string Container, string Url)> Uploads { get; } = new();
        public List<string> Deleted { get; } = new();
        public bool Fail { get; set; }

        public Task<string> UploadAsync(Stream content, string contentType, string container, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new ImageStorageUnavailableException("Image storage is currently unavailable. Please try again.");
            var url = $"https://raceday.blob.core.windows.net/{container}/{Guid.NewGuid():N}{ImageRules.ExtensionFor(contentType)}?sig=test";
            Uploads.Add((container, url));
            return Task.FromResult(url);
        }

        public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
        {
            if (url is not null) Deleted.Add(url);
            return Task.CompletedTask;
        }
    }
}
