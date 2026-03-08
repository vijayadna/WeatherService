
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using WeatherService.Application.DTO;
using WeatherService.Domain.Models;
using WeatherService.Infrastructure.Repositories;

namespace WeatherService.Application.Services
{
    public class WeatherServiceImpl : IWeatherService
    {
        public readonly IWeatherRepository _weatherRepo;
        private readonly IExternalWeatherService _external;
        private readonly ILogger<WeatherServiceImpl> _logger;

        // Cache current readings for 10 minutes before hitting external API again
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
        public WeatherServiceImpl(IWeatherRepository repo,
        IExternalWeatherService external,
        ILogger<WeatherServiceImpl> logger)
        {
            _weatherRepo = repo;
            _external = external;
            _logger = logger;
        }


        public async Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(string location, bool forceRefresh = false, CancellationToken ct = default)
        {
            // Try DB cache first
            if (!forceRefresh)
            {
                var cached = await _weatherRepo.GetLatestReadingAsync(location, ct);
                if (cached is not null && DateTime.UtcNow - cached.ObservedAtUtc < CacheDuration)
                {
                    _logger.LogDebug("Returning cached weather for {Location}", location);
                    return ToCurrentResponse(cached);
                }
            }

            // Fetch fresh from external API
            var reading = await _external.GetCurrentWeatherAsync(location, ct);
            if (reading is null) return null;

            await _weatherRepo.AddReadingAsync(reading, ct);
            return ToCurrentResponse(reading);
        }

        // ─── Forecast ─────────────────────────────────────────────────────────────

        public async Task<ForecastResponse?> GetForecastAsync(
            string location, string period, CancellationToken ct)
        {
            // Check DB for fresh forecasts (< 1 hour old)
            var existing = (await _weatherRepo.GetForecastAsync(location, period, ct)).ToList();
            if (existing.Any() && DateTime.UtcNow - existing[0].CreatedAtUtc < TimeSpan.FromHours(1))
            {
                return ToForecastResponse(location, period, existing);
            }

            // Fetch fresh
            var forecasts = period == "hourly"
                ? await _external.GetHourlyForecastAsync(location, ct)
                : await _external.GetDailyForecastAsync(location, ct);

            var list = forecasts.ToList();
            if (!list.Any()) return null;

            await _weatherRepo.UpsertForecastsAsync(list, ct);

            return ToForecastResponse(location, period, list);
        }
        // ─── CSV Export ───────────────────────────────────────────────────────────

        public async Task<byte[]> ExportToCsvAsync(
            string location, DateTime? from, DateTime? to, CancellationToken ct)
        {
            var effectiveFrom = from ?? DateTime.UtcNow.AddDays(-30);
            var effectiveTo = to ?? DateTime.UtcNow;

            var readings = await _weatherRepo.GetHistoricalAsync(
                location, effectiveFrom, effectiveTo, 1, int.MaxValue, ct);

            await using var ms = new MemoryStream();
            await using var writer = new StreamWriter(ms);
            await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteHeader<WeatherCsvRecord>();
            await csv.NextRecordAsync();

            foreach (var r in readings)
            {
                csv.WriteRecord(new WeatherCsvRecord
                {
                    Location = r.Location,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    ObservedAtUtc = r.ObservedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                    TemperatureCelsius = r.TemperatureCelsius,
                    FeelsLikeCelsius = r.FeelsLikeCelsius,
                    HumidityPercent = r.HumidityPercent,
                    WindSpeedKph = r.WindSpeedKph,
                    WindDirection = r.WindDirection,
                    PrecipitationMm = r.PrecipitationMm,
                    AirQualityIndex = r.AirQualityIndex,
                    AqiCategory = r.AqiCategory,
                    VisibilityKm = r.VisibilityKm,
                    Condition = r.Condition,
                    UvIndex = r.UvIndex,
                    Source = r.Source
                });
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync(ct);
            return ms.ToArray();
        }



        // ─── Mapping helpers ──────────────────────────────────────────────────────

        private static CurrentWeatherResponse ToCurrentResponse(WeatherReading r) =>
            new(r.Location, r.Latitude, r.Longitude,
                r.TemperatureCelsius, r.FeelsLikeCelsius, r.HumidityPercent,
                r.WindSpeedKph, r.WindDirection, r.PrecipitationMm,
                r.AirQualityIndex, r.AqiCategory, r.VisibilityKm,
                r.Condition, r.ConditionIcon, r.UvIndex,
                r.ObservedAtUtc, r.Source);

        private static ForecastResponse ToForecastResponse(
          string location, string period, IEnumerable<WeatherForecast> forecasts)
        {
            var list = forecasts.ToList();
            var first = list.FirstOrDefault();
            return new ForecastResponse(
                location,
                first?.Latitude ?? 0,
                first?.Longitude ?? 0,
                period,
                list.Select(f => new ForecastEntry(
                    f.ForecastForUtc, f.TempMinCelsius, f.TempMaxCelsius,
                    f.HumidityPercent, f.PrecipitationMm, f.PrecipitationChancePercent,
                    f.WindSpeedKph, f.Condition, f.ConditionIcon))
            );
        }
    }
    // ─── CSV Record ───────────────────────────────────────────────────────────────

    public class WeatherCsvRecord
    {
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ObservedAtUtc { get; set; } = string.Empty;
        public double TemperatureCelsius { get; set; }
        public double FeelsLikeCelsius { get; set; }
        public int HumidityPercent { get; set; }
        public double WindSpeedKph { get; set; }
        public string WindDirection { get; set; } = string.Empty;
        public double PrecipitationMm { get; set; }
        public int AirQualityIndex { get; set; }
        public string AqiCategory { get; set; } = string.Empty;
        public double VisibilityKm { get; set; }
        public string Condition { get; set; } = string.Empty;
        public int UvIndex { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
