using System.Globalization;
using System.Text.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Services;

/// <summary>Fetches live weather for a coordinate from an external provider.</summary>
public interface IWeatherService
{
    Task<WeatherResponse> GetCurrentAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}

/// <summary>Raised when the external weather provider cannot be reached or returns bad data. Mapped to 503.</summary>
public class WeatherUnavailableException : Exception
{
    public WeatherUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Open-Meteo (https://open-meteo.com) client. It is free and needs no API key, so the project runs
/// for the marker without any secret configuration.
/// </summary>
public class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _http;

    public OpenMeteoWeatherService(HttpClient http)
    {
        _http = http;
    }

    public async Task<WeatherResponse> GetCurrentAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"v1/forecast?latitude={lat}&longitude={lon}" +
                  "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code&timezone=Africa%2FJohannesburg";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new WeatherUnavailableException($"Weather provider returned {(int)response.StatusCode}.");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var current = doc.RootElement.GetProperty("current");
            var code = current.GetProperty("weather_code").GetInt32();

            return new WeatherResponse
            {
                Latitude = latitude,
                Longitude = longitude,
                ObservedAt = current.GetProperty("time").GetString(),
                TemperatureC = current.GetProperty("temperature_2m").GetDecimal(),
                HumidityPercent = current.GetProperty("relative_humidity_2m").GetInt32(),
                WindSpeedKmh = current.GetProperty("wind_speed_10m").GetDecimal(),
                WeatherCode = code,
                Conditions = DescribeCode(code),
                Source = "Open-Meteo"
            };
        }
        catch (WeatherUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new WeatherUnavailableException("The weather service is currently unavailable.", ex);
        }
    }

    /// <summary>Translates WMO weather interpretation codes into readable text.</summary>
    public static string DescribeCode(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 => "Light rain",
        63 => "Moderate rain",
        65 => "Heavy rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown"
    };
}
