namespace RaceDay.Api.Dtos;

/// <summary>A simple confirmation or error message.</summary>
/// <param name="Message">Human-readable description of what happened.</param>
public record MessageResponse(string Message);

/// <summary>A single image sent as multipart/form-data in a field named "file".</summary>
public class ImageUploadRequest
{
    /// <summary>JPG, PNG or WebP image, 5 MB maximum.</summary>
    public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
}
