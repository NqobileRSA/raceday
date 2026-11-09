using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

/// <summary>Stores uploaded images and returns a URL the browser can display.</summary>
public interface IImageStorage
{
    /// <param name="content">The image bytes.</param>
    /// <param name="contentType">Validated MIME type, e.g. image/jpeg.</param>
    /// <param name="container">Logical folder, e.g. <see cref="ImageContainers.EventBanners"/>.</param>
    /// <param name="cancellationToken">Cancelled if the client disconnects.</param>
    /// <returns>The URL saved to the database and shown in the MVC app.</returns>
    Task<string> UploadAsync(Stream content, string contentType, string container, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously uploaded image. Unknown or external URLs are ignored.</summary>
    Task DeleteAsync(string? url, CancellationToken cancellationToken = default);
}

public static class ImageContainers
{
    public const string EventBanners = "event-banners";
    public const string ProfilePictures = "profile-pictures";
}

/// <summary>Raised when Azure Blob Storage cannot be reached. Mapped to 503 by the controllers.</summary>
public class ImageStorageUnavailableException : Exception
{
    public ImageStorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Azure Blob Storage implementation. Containers are private; each upload gets a read-only SAS URL
/// valid for several years, so images display without enabling anonymous access on the storage account.
/// Works the same against a real Azure account and the Azurite emulator used by docker-compose.
/// </summary>
public class AzureBlobImageStorage : IImageStorage
{
    private readonly BlobServiceClient _service;
    private readonly ILogger<AzureBlobImageStorage> _logger;
    private readonly string? _publicEndpoint;
    private readonly TimeSpan _sasLifetime;

    public AzureBlobImageStorage(BlobServiceClient service, IConfiguration config, ILogger<AzureBlobImageStorage> logger)
    {
        _service = service;
        _logger = logger;
        // Inside Docker the API reaches Azurite at http://azurite:10000 but the browser needs http://localhost:10000.
        _publicEndpoint = config["AzureStorage:PublicBlobEndpoint"]?.TrimEnd('/');
        _sasLifetime = TimeSpan.FromDays(config.GetValue("AzureStorage:SasLifetimeDays", 365 * 5));
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string container, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _service.GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ImageRules.ExtensionFor(contentType)}";
            var blob = containerClient.GetBlobClient(blobName);
            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = "public, max-age=31536000" }
            }, cancellationToken);

            var uri = blob.CanGenerateSasUri
                ? blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(_sasLifetime))
                : blob.Uri;

            return ToPublicUrl(uri.ToString());
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException or HttpRequestException or AggregateException)
        {
            _logger.LogError(ex, "Upload to blob container {Container} failed", container);
            throw new ImageStorageUnavailableException("Image storage is currently unavailable. Please try again.", ex);
        }
    }

    public async Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var path = new Uri(url).AbsolutePath.TrimStart('/');
            // Path is "<account>/<container>/<blob>" for Azurite and "<container>/<blob>" for Azure.
            var parts = path.Split('/', 2);
            if (parts.Length == 2 && parts[0] == _service.AccountName) parts = parts[1].Split('/', 2);
            if (parts.Length != 2) return;
            await _service.GetBlobContainerClient(parts[0]).DeleteBlobIfExistsAsync(Uri.UnescapeDataString(parts[1]), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Never fail a request because an old image could not be removed.
            _logger.LogWarning(ex, "Could not delete old image {Url}", url);
        }
    }

    private string ToPublicUrl(string url)
    {
        if (string.IsNullOrEmpty(_publicEndpoint)) return url;
        var serviceRoot = _service.Uri.ToString().TrimEnd('/');
        return url.StartsWith(serviceRoot, StringComparison.OrdinalIgnoreCase)
            ? _publicEndpoint + url[serviceRoot.Length..]
            : url;
    }
}

/// <summary>Validation shared by every upload endpoint.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public static string ExtensionFor(string contentType) => Extensions.TryGetValue(contentType, out var ext) ? ext : ".img";

    /// <summary>Returns an error message, or null when the file is an acceptable image.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image to upload.";
        if (file.Length > MaxBytes) return "Images must be 5 MB or smaller.";
        if (!Extensions.ContainsKey(file.ContentType ?? "")) return "Only JPG, PNG or WebP images are allowed.";

        // Check the file signature so a renamed non-image cannot be uploaded.
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = stream.Read(header, 0, header.Length);
        var isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isWebp = read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                     && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
        return isJpeg || isPng || isWebp ? null : "The file is not a valid JPG, PNG or WebP image.";
    }
}
