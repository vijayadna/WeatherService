using WeatherService.Application.DTO;

namespace WeatherService.Application.Services
{
    public interface IWeatherService
    {
        Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(string location, bool forceRefresh = false, CancellationToken ct = default);
        Task<ForecastResponse?> GetForecastAsync(string location, string period = "daily", CancellationToken ct = default);
        Task<byte[]> ExportToCsvAsync(string location, DateTime? from, DateTime? to, CancellationToken ct = default);
    }
}
