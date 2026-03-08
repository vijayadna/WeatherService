using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using WeatherService.Domain.Models;

namespace WeatherService.Application.Services
{
    public interface IExternalWeatherService
    {
        Task<WeatherReading?> GetCurrentWeatherAsync(string location, CancellationToken ct = default);
        Task<IEnumerable<WeatherForecast>> GetHourlyForecastAsync(string location, CancellationToken ct = default);
        Task<IEnumerable<WeatherForecast>> GetDailyForecastAsync(string location, CancellationToken ct = default);
    }


    public class OpenWeatherMapService : IExternalWeatherService
    {
        private readonly HttpClient _http;
        private readonly ILogger<OpenWeatherMapService> _logger;
        private readonly string _apiKey;
        private const string Source = "openweathermap";
        public OpenWeatherMapService(
        HttpClient http,
        IConfiguration config,
        ILogger<OpenWeatherMapService> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["ExternalApis:OpenWeatherMap:ApiKey"]
                      ?? throw new InvalidOperationException("OpenWeatherMap API key not configured.");
        }

        public async Task<WeatherReading?> GetCurrentWeatherAsync(string location, CancellationToken ct)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(location)}&appid={_apiKey}&units=metric";
                var response = await _http.GetFromJsonAsync<JsonElement>(url, ct);

                var aqi = await GetAirQualityAsync(
                    response.GetProperty("coord").GetProperty("lat").GetDouble(),
                    response.GetProperty("coord").GetProperty("lon").GetDouble(), ct);

                return MapToReading(response, aqi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch current weather for {Location}", location);
                return null;
            }
        }

        public async Task<IEnumerable<WeatherForecast>> GetHourlyForecastAsync(string location, CancellationToken ct)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={Uri.EscapeDataString(location)}&appid={_apiKey}&units=metric&cnt=48";
                var response = await _http.GetFromJsonAsync<JsonElement>(url, ct);
                return MapToForecasts(response, location, "hourly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch hourly forecast for {Location}", location);
                return Enumerable.Empty<WeatherForecast>();
            }
        }

        public async Task<IEnumerable<WeatherForecast>> GetDailyForecastAsync(string location, CancellationToken ct)
        {
            try
            {
                // OWM free tier returns 3h blocks; we group by day
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={Uri.EscapeDataString(location)}&appid={_apiKey}&units=metric&cnt=40";
                var response = await _http.GetFromJsonAsync<JsonElement>(url, ct);
                return MapToDailyForecasts(response, location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch daily forecast for {Location}", location);
                return Enumerable.Empty<WeatherForecast>();
            }
        }
        // ─── Private helpers ──────────────────────────────────────────────────────

        private async Task<(int aqi, string category)> GetAirQualityAsync(double lat, double lon, CancellationToken ct)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={lat}&lon={lon}&appid={_apiKey}";
                var response = await _http.GetFromJsonAsync<JsonElement>(url, ct);
                var aqiValue = response.GetProperty("list")[0].GetProperty("main").GetProperty("aqi").GetInt32();
                var category = aqiValue switch
                {
                    1 => "Good",
                    2 => "Fair",
                    3 => "Moderate",
                    4 => "Poor",
                    5 => "Very Poor",
                    _ => "Unknown"
                };
                return (aqiValue * 50, category); // scale to 0-500 AQI range
            }
            catch
            {
                return (0, "Unknown");
            }
        }

        private WeatherReading MapToReading(JsonElement data, (int aqi, string category) airQuality)
        {
            var wind = data.GetProperty("wind");
            var main = data.GetProperty("main");
            var weather = data.GetProperty("weather")[0];
            var coord = data.GetProperty("coord");

            var windDeg = wind.TryGetProperty("deg", out var deg) ? deg.GetDouble() : 0;

            return new WeatherReading
            {
                Location = data.GetProperty("name").GetString() ?? "Unknown",
                Latitude = coord.GetProperty("lat").GetDouble(),
                Longitude = coord.GetProperty("lon").GetDouble(),
                TemperatureCelsius = main.GetProperty("temp").GetDouble(),
                FeelsLikeCelsius = main.GetProperty("feels_like").GetDouble(),
                HumidityPercent = main.GetProperty("humidity").GetInt32(),
                WindSpeedKph = wind.GetProperty("speed").GetDouble() * 3.6,
                WindDirection = DegreesToDirection(windDeg),
                PrecipitationMm = data.TryGetProperty("rain", out var rain) && rain.TryGetProperty("1h", out var r1h) ? r1h.GetDouble() : 0,
                AirQualityIndex = airQuality.aqi,
                AqiCategory = airQuality.category,
                VisibilityKm = data.TryGetProperty("visibility", out var vis) ? vis.GetDouble() / 1000 : 0,
                Condition = weather.GetProperty("main").GetString() ?? "Unknown",
                ConditionIcon = $"https://openweathermap.org/img/wn/{weather.GetProperty("icon").GetString()}@2x.png",
                UvIndex = 0, // Requires separate UV API call
                Source = Source,
                ObservedAtUtc = DateTimeOffset.FromUnixTimeSeconds(data.GetProperty("dt").GetInt64()).UtcDateTime
            };
        }

        private IEnumerable<WeatherForecast> MapToForecasts(JsonElement data, string location, string period)
        {
            var list = data.GetProperty("list");
            var coord = data.GetProperty("city").GetProperty("coord");
            var lat = coord.GetProperty("lat").GetDouble();
            var lon = coord.GetProperty("lon").GetDouble();

            foreach (var item in list.EnumerateArray())
            {
                var main = item.GetProperty("main");
                var weather = item.GetProperty("weather")[0];
                var wind = item.GetProperty("wind");
                var pop = item.TryGetProperty("pop", out var p) ? (int)(p.GetDouble() * 100) : 0;
                var rain = item.TryGetProperty("rain", out var r) && r.TryGetProperty("3h", out var r3h) ? r3h.GetDouble() : 0;

                yield return new WeatherForecast
                {
                    Location = location,
                    Latitude = lat,
                    Longitude = lon,
                    ForecastForUtc = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).UtcDateTime,
                    Period = period,
                    TempMinCelsius = main.GetProperty("temp_min").GetDouble(),
                    TempMaxCelsius = main.GetProperty("temp_max").GetDouble(),
                    HumidityPercent = main.GetProperty("humidity").GetInt32(),
                    PrecipitationMm = rain,
                    PrecipitationChancePercent = pop,
                    WindSpeedKph = wind.GetProperty("speed").GetDouble() * 3.6,
                    Condition = weather.GetProperty("main").GetString() ?? "Unknown",
                    ConditionIcon = $"https://openweathermap.org/img/wn/{weather.GetProperty("icon").GetString()}@2x.png",
                    Source = Source
                };
            }
        }

        private IEnumerable<WeatherForecast> MapToDailyForecasts(JsonElement data, string location)
        {
            var hourlyForecasts = MapToForecasts(data, location, "daily").ToList();

            return hourlyForecasts
                .GroupBy(f => f.ForecastForUtc.Date)
                .Select(g => new WeatherForecast
                {
                    Location = location,
                    Latitude = g.First().Latitude,
                    Longitude = g.First().Longitude,
                    ForecastForUtc = g.Key.ToUniversalTime(),
                    Period = "daily",
                    TempMinCelsius = g.Min(f => f.TempMinCelsius),
                    TempMaxCelsius = g.Max(f => f.TempMaxCelsius),
                    HumidityPercent = (int)g.Average(f => f.HumidityPercent),
                    PrecipitationMm = g.Sum(f => f.PrecipitationMm),
                    PrecipitationChancePercent = (int)g.Max(f => f.PrecipitationChancePercent),
                    WindSpeedKph = g.Max(f => f.WindSpeedKph),
                    Condition = g.First().Condition,
                    ConditionIcon = g.First().ConditionIcon,
                    Source = Source
                });
        }

        private static string DegreesToDirection(double degrees) => degrees switch
        {
            >= 337.5 or < 22.5 => "N",
            >= 22.5 and < 67.5 => "NE",
            >= 67.5 and < 112.5 => "E",
            >= 112.5 and < 157.5 => "SE",
            >= 157.5 and < 202.5 => "S",
            >= 202.5 and < 247.5 => "SW",
            >= 247.5 and < 292.5 => "W",
            >= 292.5 and < 337.5 => "NW",
            _ => "N"
        };


    }


}
