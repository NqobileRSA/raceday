namespace RaceDay.Api.Dtos;

/// <summary>A simple confirmation or error message.</summary>
/// <param name="Message">Human-readable description of what happened.</param>
public record MessageResponse(string Message);
