using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaceDay.Web.Infrastructure;

/// <summary>The outcome of one API call: either the data, or an HTTP status with a readable message.</summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;

    /// <summary>Field-level validation messages from the API, keyed by snake_case field name.</summary>
    public Dictionary<string, string[]> FieldErrors { get; init; } = new();

    public static ApiResult<T> Ok(T? data, int status) => new() { Success = true, StatusCode = status, Data = data };
    public static ApiResult<T> Fail(int status, string message, Dictionary<string, string[]>? fields = null) =>
        new() { Success = false, StatusCode = status, Message = message, FieldErrors = fields ?? new() };
}

/// <summary>
/// Typed HTTP client for the RaceDay API. The API keeps the login in its own server-side session and
/// identifies the browser by the ".RaceDay.Session" cookie. This client stores that cookie value in the
/// MVC session after login and sends it with every request, so each MVC user has their own API session.
/// </summary>
public class ApiClient
{
    public const string ApiCookieName = ".RaceDay.Session";

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, IHttpContextAccessor context, ILogger<ApiClient> logger)
    {
        _http = http;
        _context = context;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url, null);

    /// <summary>Sends an uploaded file on to the API as multipart/form-data (field name "file").</summary>
    public async Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", Path.GetFileName(file.FileName));
        return await SendAsync<T>(HttpMethod.Post, url, content);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url.TrimStart('/'));
        if (body is HttpContent httpContent) request.Content = httpContent;
        else if (body is not null) request.Content = JsonContent.Create(body, options: Json);

        var session = _context.HttpContext?.Session;
        var apiCookie = session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie)) request.Headers.Add("Cookie", $"{ApiCookieName}={apiCookie}");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "RaceDay API unreachable: {Method} {Url}", method, url);
            return ApiResult<T>.Fail(503, "RaceDay is temporarily unavailable. Please try again in a moment.");
        }

        using (response)
        {
            CaptureSessionCookie(response, session);
            var status = (int)response.StatusCode;
            var text = response.Content is null ? "" : await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(text) || typeof(T) == typeof(object)) return ApiResult<T>.Ok(default, status);
                try
                {
                    return ApiResult<T>.Ok(JsonSerializer.Deserialize<T>(text, Json), status);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Unexpected response from {Url}", url);
                    return ApiResult<T>.Fail(502, "RaceDay returned an unexpected response. Please try again.");
                }
            }

            var (message, fields) = ReadError(text, response.StatusCode);
            return ApiResult<T>.Fail(status, message, fields);
        }
    }

    /// <summary>Stores the API session cookie from login/register so later requests are authenticated.</summary>
    private static void CaptureSessionCookie(HttpResponseMessage response, ISession? session)
    {
        if (session is null || !response.Headers.TryGetValues("Set-Cookie", out var cookies)) return;
        foreach (var cookie in cookies)
        {
            if (!cookie.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;
            var value = cookie[(ApiCookieName.Length + 1)..].Split(';')[0];
            if (!string.IsNullOrEmpty(value)) session.SetString(SessionKeys.ApiCookie, value);
        }
    }

    /// <summary>Turns API error bodies (MessageResponse or ValidationProblemDetails) into user-friendly text.</summary>
    private static (string Message, Dictionary<string, string[]> Fields) ReadError(string text, HttpStatusCode status)
    {
        var fields = new Dictionary<string, string[]>();
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in errors.EnumerateObject())
                            fields[p.Name] = p.Value.EnumerateArray().Select(v => v.GetString() ?? "").ToArray();
                        return ("Please check the highlighted fields.", fields);
                    }
                    if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        return (m.GetString()!, fields);
                }
            }
            catch (JsonException)
            {
                // Not JSON: fall through to a generic message so raw server text is never shown.
            }
        }

        return ((int)status switch
        {
            401 => "Please log in to continue.",
            403 => "You don't have access to that.",
            404 => "We couldn't find what you were looking for.",
            >= 500 => "Something went wrong on our side. Please try again.",
            _ => "That request couldn't be completed. Please check your details."
        }, fields);
    }
}
